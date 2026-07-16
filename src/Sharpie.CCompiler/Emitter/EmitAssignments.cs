using ClangSharp.Interop;
using Sharpie.CCompiler.Emitter;

namespace Sharpie.CCompiler;

public partial class SharpieEmitter
{
    private static void EmitAssignmentStatement(CXCursor assignmentCursor, EmissionContext context)
    {
        var children = GetChildren(assignmentCursor);
        if (children.Count != 2)
            throw new InvalidOperationException("Expected assignment to have exactly 2 operands.");

        var lhs = PeelExpression(children[0]);
        var rhs = PeelExpression(children[1]);

        if (assignmentCursor.Kind == CXCursorKind.CXCursor_CompoundAssignOperator)
        {
            EmitCompoundAssignment(assignmentCursor, lhs, rhs, context);
            return;
        }

        if (lhs.Kind == CXCursorKind.CXCursor_DeclRefExpr)
        {
            var variableName = lhs.Spelling.ToString();

            if (
                context.Locals.TryGetValue(variableName, out var loc)
                && loc.Type == StorageType.Register
            )
            {
                if (
                    lhs.Type.SizeOf == 1
                    && rhs.Kind != CXCursorKind.CXCursor_CallExpr
                    && rhs.Kind != CXCursorKind.CXCursor_IntegerLiteral
                    && rhs.Kind != CXCursorKind.CXCursor_DeclRefExpr
                )
                {
                    using var valueReg = context.AcquireTempRegister();
                    EmitExpression(rhs, valueReg.Value, context);
                    TruncateToByteIfNeeded(valueReg.Value, 1, rhs, context);
                    context.Emit($"MOV r{loc.Value}, r{valueReg.Value}");
                }
                else
                {
                    EmitExpression(rhs, loc.Value, context);
                    TruncateToByteIfNeeded(loc.Value, lhs.Type.SizeOf, rhs, context);
                }
                return;
            }
            else if (context.Globals.Contains(variableName))
            {
                var globalSize = lhs.Type.SizeOf;
                if (globalSize <= 2)
                {
                    using var valueReg = context.AcquireTempRegister();
                    EmitExpression(rhs, valueReg.Value, context);

                    var prefix = (globalSize == 1) ? "ALT " : "";
                    context.Emit($"{prefix}STM r{valueReg.Value}, _global_{variableName}");
                    return;
                }
            }
        }

        var assignSize = lhs.Type.SizeOf;
        if (assignSize > 2)
        {
            var peeledRhs = PeelExpression(rhs);
            if (peeledRhs.Kind == CXCursorKind.CXCursor_CallExpr && peeledRhs.Type.SizeOf > 2)
            {
                using var destAddrReg = context.AcquireTempRegister();
                EmitLValueAddress(lhs, destAddrReg.Value, context);

                EmitCallExpressionInto(peeledRhs, destAddrReg.Value, context);
                return;
            }

            if (peeledRhs.Kind == CXCursorKind.CXCursor_CompoundLiteralExpr)
            {
                using var destAddrReg = context.AcquireTempRegister();
                EmitLValueAddress(lhs, destAddrReg.Value, context);
                EmitCompoundLiteralIntoAddress(peeledRhs, destAddrReg.Value, context);
                return;
            }

            if (lhs.Type.CanonicalType.kind == CXTypeKind.CXType_Record)
            {
                using var srcAddrReg = context.AcquireTempRegister();
                EmitExpression(rhs, srcAddrReg.Value, context);

                using var destAddrReg2 = context.AcquireTempRegister();
                EmitLValueAddress(lhs, destAddrReg2.Value, context);

                if (assignSize <= 4)
                {
                    EmitInlineAggregateCopy(srcAddrReg.Value, destAddrReg2.Value, (int)assignSize, context);
                }
                else
                {
                    context.Emit($"PUSH r{srcAddrReg.Value}");
                    context.Emit($"MOV r1, r{destAddrReg2.Value}");
                    context.Emit("POP r2");
                    context.Emit($"LDI r3, {assignSize}");
                    context.Emit("CALL SYS_MEM_COPY");
                }
            }
            else
            {
                using var destAddrReg2 = context.AcquireTempRegister();
                EmitLValueAddress(lhs, destAddrReg2.Value, context);

                EmitLongToAddress(rhs, destAddrReg2.Value, context);
            }
            return;
        }

        using var valReg = context.AcquireTempRegister();
        EmitExpression(rhs, valReg.Value, context);

        using var addrReg = context.AcquireTempRegister();
        EmitLValueAddress(lhs, addrReg.Value, context);

        var storePrefix = (assignSize == 1) ? "ALT " : "";
        context.Emit($"{storePrefix}STA r{valReg.Value}, r{addrReg.Value}");
    }

    private static void EmitInlineAggregateCopy(
        int srcAddrReg,
        int destAddrReg,
        int sizeBytes,
        EmissionContext context
    )
    {
        using var srcPtr = context.AcquireTempRegister();
        using var dstPtr = context.AcquireTempRegister();
        using var valReg = context.AcquireTempRegister();

        context.Emit($"MOV r{srcPtr.Value}, r{srcAddrReg}");
        context.Emit($"MOV r{dstPtr.Value}, r{destAddrReg}");

        var remaining = sizeBytes;
        while (remaining >= 2)
        {
            context.Emit($"LDP r{valReg.Value}, r{srcPtr.Value}");
            context.Emit($"STA r{valReg.Value}, r{dstPtr.Value}");

            remaining -= 2;
            if (remaining > 0)
            {
                context.Emit($"IADD r{srcPtr.Value}, 2");
                context.Emit($"IADD r{dstPtr.Value}, 2");
            }
        }

        if (remaining == 1)
        {
            context.Emit($"ALT LDP r{valReg.Value}, r{srcPtr.Value}");
            context.Emit($"ALT STA r{valReg.Value}, r{dstPtr.Value}");
        }
    }

    private static void EmitCompoundAssignment(
        CXCursor assignmentCursor,
        CXCursor lhs,
        CXCursor rhs,
        EmissionContext context
    )
    {
        var kind = GetBinaryOperatorKind(assignmentCursor);
        int sizeBytes = (int)assignmentCursor.Type.SizeOf;

        if (sizeBytes > 2)
        {
            EmitCompoundAssignmentLong(lhs, rhs, kind, context);
            return;
        }

        using var mathRegLease = context.AcquireTempRegister();
        int mathReg = mathRegLease.Value;
        EmitExpression(lhs, mathReg, context);

        if (!TryEmitImmediateMath(kind, mathReg, rhs, context))
        {
            using var scratch = context.AcquireTempRegister();
            EmitExpression(rhs, scratch.Value, context);

            var op = kind switch
            {
                CXBinaryOperatorKind.CXBinaryOperator_AddAssign => "ADD",
                CXBinaryOperatorKind.CXBinaryOperator_SubAssign => "SUB",
                CXBinaryOperatorKind.CXBinaryOperator_MulAssign => "MUL",
                CXBinaryOperatorKind.CXBinaryOperator_DivAssign => "DIV",
                CXBinaryOperatorKind.CXBinaryOperator_RemAssign => "MOD",
                CXBinaryOperatorKind.CXBinaryOperator_AndAssign => "AND",
                CXBinaryOperatorKind.CXBinaryOperator_OrAssign => "OR",
                CXBinaryOperatorKind.CXBinaryOperator_XorAssign => "XOR",
                CXBinaryOperatorKind.CXBinaryOperator_ShlAssign => "SHL",
                CXBinaryOperatorKind.CXBinaryOperator_ShrAssign => "SHR",
                _ => throw new InvalidOperationException(
                    $"Unsupported compound assignment: {kind}"
                ),
            };

            context.Emit($"{op} r{mathReg}, r{scratch.Value}");
        }

        if (lhs.Kind == CXCursorKind.CXCursor_DeclRefExpr)
        {
            var name = lhs.Spelling.ToString();

            if (context.Locals.TryGetValue(name, out var loc) && loc.Type == StorageType.Register)
            {
                if (mathReg != loc.Value)
                    context.Emit($"MOV r{loc.Value}, r{mathReg}");
                TruncateToByte(loc.Value, lhs.Type.SizeOf, context);
                return;
            }
            else if (context.Globals.Contains(name))
            {
                var isByte = lhs.Type.SizeOf == 1;
                var prefix = isByte ? "ALT " : "";
                context.Emit($"{prefix}STM r{mathReg}, _global_{name}");
                return;
            }
        }

        using var addrReg = context.AcquireTempRegister();
        EmitLValueAddress(lhs, addrReg.Value, context);

        var isByteFallback = lhs.Type.SizeOf == 1;
        var prefixFallback = isByteFallback ? "ALT " : "";
        context.Emit($"{prefixFallback}STA r{mathReg}, r{addrReg.Value}");
    }

    private static void EmitCompoundAssignmentLong(
        CXCursor lhs,
        CXCursor rhs,
        CXBinaryOperatorKind kind,
        EmissionContext context
    )
    {
        using var addrReg = context.AcquireTempRegister();
        EmitLValueAddress(lhs, addrReg.Value, context);

        using var lhsLow = context.AcquireTempRegister();
        using var lhsHigh = context.AcquireTempRegister();
        context.Emit($"LDP r{lhsLow.Value}, r{addrReg.Value}");
        context.Emit($"IADD r{addrReg.Value}, 2");
        context.Emit($"LDP r{lhsHigh.Value}, r{addrReg.Value}");
        context.Emit($"IADD r{addrReg.Value}, -2");

        string cmpName = kind switch
        {
            CXBinaryOperatorKind.CXBinaryOperator_AddAssign => "add",
            CXBinaryOperatorKind.CXBinaryOperator_SubAssign => "sub",
            CXBinaryOperatorKind.CXBinaryOperator_MulAssign => "mul",
            CXBinaryOperatorKind.CXBinaryOperator_AndAssign => "and",
            CXBinaryOperatorKind.CXBinaryOperator_OrAssign => "or",
            CXBinaryOperatorKind.CXBinaryOperator_XorAssign => "xor",
            CXBinaryOperatorKind.CXBinaryOperator_ShlAssign => "shl",
            CXBinaryOperatorKind.CXBinaryOperator_ShrAssign => "shr",
            CXBinaryOperatorKind.CXBinaryOperator_DivAssign => "div",
            CXBinaryOperatorKind.CXBinaryOperator_RemAssign => "mod",
            _ => throw new InvalidOperationException($"Unsupported long compound assignment: {kind}")
        };

        if (kind is CXBinaryOperatorKind.CXBinaryOperator_ShlAssign or CXBinaryOperatorKind.CXBinaryOperator_ShrAssign)
        {
            var peeledRhs = PeelExpression(rhs);
            if (peeledRhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
            {
                long sv = peeledRhs.Evaluate.AsLongLong;
                if (sv == 0)
                {
                    context.Emit($"STA r{lhsLow.Value}, r{addrReg.Value}");
                    context.Emit($"IADD r{addrReg.Value}, 2");
                    context.Emit($"STA r{lhsHigh.Value}, r{addrReg.Value}");
                    return;
                }
                context.Emit($"LDI r4, {unchecked((ushort)sv)}");
            }
            else
            {
                using var src = context.AcquireTempRegister();
                EmitExpression(rhs, src.Value, context);
                context.Emit($"MOV r4, r{src.Value}");
            }
            context.Emit($"XOR r3, r3");
        }
        else
        {
            LoadLongToHighLow(rhs, PeelExpression(rhs), 3, 4, context);
        }

        context.Emit("MOV r0, r{addrReg.Value}");
        context.Emit($"MOV r1, r{lhsHigh.Value}");
        context.Emit($"MOV r2, r{lhsLow.Value}");

        context.Emit($"CALL _func___injected_32bit_{cmpName}");

        context.Emit("STA r2, r0");
        context.Emit("IADD r0, 2");
        context.Emit("STA r1, r0");
    }

    private static void EmitVariableDeclaration(CXCursor varDecl, EmissionContext context)
    {
        var variableName = varDecl.Spelling.ToString();
        if (string.IsNullOrWhiteSpace(variableName))
            throw new InvalidOperationException("Encountered unnamed local variable.");

        var typeKind = varDecl.Type.CanonicalType.kind;
        bool isRecord = typeKind == CXTypeKind.CXType_Record;
        bool isArray =
            typeKind == CXTypeKind.CXType_ConstantArray
            || typeKind == CXTypeKind.CXType_IncompleteArray;
        long sizeBytes = varDecl.Type.SizeOf;

        if (sizeBytes < 0)
            throw new InvalidOperationException($"Cannot determine size for `{varDecl.Spelling}`.");

        var needsStack = isRecord || isArray || context.EscapedVariables.Contains(variableName);
        var space = context.AllocateStorage(variableName, needsStack, (int)sizeBytes);

        var initExprs = GetChildren(varDecl)
            .Where(c =>
                c.Kind >= CXCursorKind.CXCursor_FirstExpr
                && c.Kind <= CXCursorKind.CXCursor_LastExpr
            )
            .ToList();

        if (isArray && initExprs.Count > 0)
        {
            var lastExpr = PeelExpression(initExprs[^1]);
            if (lastExpr.Kind == CXCursorKind.CXCursor_StringLiteral)
            {
                var sourceLabel = GetOrAddStringLiteral(lastExpr, context);

                int byteLength = (int)lastExpr.Type.SizeOf;

                using var addrReg = context.AcquireTempRegister();

                if (space.Type == StorageType.Stack)
                {
                    context.Emit($"MOV r{addrReg.Value}, r15");
                    AccumulateOffset(addrReg.Value, space.Value, context);
                }
                else
                {
                    context.Emit($"MOV r{addrReg.Value}, r{space.Value}");
                }

                if (addrReg.Value != 1)
                    context.Emit($"MOV r1, r{addrReg.Value}");

                context.Emit($"LDI r2, {sourceLabel}");
                context.Emit($"LDI r3, {byteLength}");
                context.Emit("CALL SYS_MEM_COPY");
                return;
            }
        }

        var initList = GetChildren(varDecl)
            .FirstOrDefault(c => c.Kind == CXCursorKind.CXCursor_InitListExpr);
        var hasInitList = initList.Kind == CXCursorKind.CXCursor_InitListExpr;

        if (isRecord || isArray)
        {
            if (hasInitList)
            {
                var initVals = GetChildren(initList);

                using var baseReg = context.AcquireTempRegister();
                context.Emit($"MOV r{baseReg.Value}, r15");
                AccumulateOffset(baseReg.Value, space.Value, context);

                if (isArray)
                {
                    var elementType = clang.getElementType(varDecl.Type);
                    var elementCanonical = elementType.CanonicalType;
                    bool elementIsRecord =
                        elementType.kind == CXTypeKind.CXType_Record
                        || elementCanonical.kind == CXTypeKind.CXType_Record;
                    bool elementIsArray =
                        elementType.kind == CXTypeKind.CXType_ConstantArray
                        || elementType.kind == CXTypeKind.CXType_IncompleteArray;
                    long stride = elementType.SizeOf;
                    if (stride <= 0)
                        stride = 2;

                    if (elementIsRecord)
                    {
                        var decl = clang.getTypeDeclaration(elementCanonical);
                        var fields = GetChildren(decl)
                            .Where(c => c.Kind == CXCursorKind.CXCursor_FieldDecl)
                            .ToList();

                        for (int i = 0; i < initVals.Count; i++)
                        {
                            var fieldInitVals = GetAggregateInitializerValues(initVals[i]);

                            for (int fieldIndex = 0; fieldIndex < fieldInitVals.Count && fieldIndex < fields.Count; fieldIndex++)
                            {
                                long fieldOffset = clang.Cursor_getOffsetOfField(fields[fieldIndex]) / 8;
                                long fieldSize = fields[fieldIndex].Type.SizeOf <= 0 ? 2 : fields[fieldIndex].Type.SizeOf;

                                using var valReg = context.AcquireTempRegister();
                                EmitExpression(fieldInitVals[fieldIndex], valReg.Value, context);

                                using var addrReg = context.AcquireTempRegister();
                                context.Emit($"MOV r{addrReg.Value}, r{baseReg.Value}");
                                AccumulateOffset(addrReg.Value, (int)((i * stride) + fieldOffset), context);

                                string altPrefix = (fieldSize == 1) ? "ALT " : "";
                                context.Emit($"{altPrefix}STA r{valReg.Value}, r{addrReg.Value}");
                            }
                        }
                    }
                    else if (elementIsArray)
                    {
                        var innerElementType = clang.getElementType(elementType);
                        long innerStride = innerElementType.SizeOf <= 0 ? 2 : innerElementType.SizeOf;

                        for (int i = 0; i < initVals.Count; i++)
                        {
                            var innerInitVals = GetAggregateInitializerValues(initVals[i]);

                            for (int j = 0; j < innerInitVals.Count; j++)
                            {
                                using var valReg = context.AcquireTempRegister();
                                EmitExpression(innerInitVals[j], valReg.Value, context);

                                using var addrReg = context.AcquireTempRegister();
                                context.Emit($"MOV r{addrReg.Value}, r{baseReg.Value}");
                                AccumulateOffset(addrReg.Value, (int)((i * stride) + (j * innerStride)), context);

                                string altPrefix = (innerStride == 1) ? "ALT " : "";
                                context.Emit($"{altPrefix}STA r{valReg.Value}, r{addrReg.Value}");
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < initVals.Count; i++)
                        {
                            using var valReg = context.AcquireTempRegister();
                            EmitExpression(initVals[i], valReg.Value, context);

                            using var addrReg = context.AcquireTempRegister();
                            context.Emit($"MOV r{addrReg.Value}, r{baseReg.Value}");
                            AccumulateOffset(addrReg.Value, (int)(i * stride), context);

                            string altPrefix = (stride == 1) ? "ALT " : "";
                            context.Emit($"{altPrefix}STA r{valReg.Value}, r{addrReg.Value}");
                        }
                    }
                }
                else
                {
                    var decl = clang.getTypeDeclaration(varDecl.Type.CanonicalType);
                    var fields = GetChildren(decl)
                        .Where(c => c.Kind == CXCursorKind.CXCursor_FieldDecl)
                        .ToList();

                    for (int i = 0; i < initVals.Count && i < fields.Count; i++)
                    {
                        long offsetBytes = clang.Cursor_getOffsetOfField(fields[i]) / 8;
                        long fieldSize = fields[i].Type.SizeOf;

                        using var valReg = context.AcquireTempRegister();
                        EmitExpression(initVals[i], valReg.Value, context);

                        using var addrReg = context.AcquireTempRegister();
                        context.Emit($"MOV r{addrReg.Value}, r{baseReg.Value}");
                        AccumulateOffset(addrReg.Value, (int)offsetBytes, context);

                        string altPrefix = (fieldSize == 1) ? "ALT " : "";
                        context.Emit($"{altPrefix}STA r{valReg.Value}, r{addrReg.Value}");
                    }
                }
            }
            else if (!isArray && initExprs.Count > 0)
            {
                var initExpr = PeelExpression(initExprs[^1]);

                if (isRecord && initExpr.Kind == CXCursorKind.CXCursor_CallExpr && initExpr.Type.SizeOf > 2)
                {
                    using var dest = context.AcquireTempRegister();
                    context.Emit($"MOV r{dest.Value}, r15");
                    AccumulateOffset(dest.Value, space.Value, context);

                    EmitCallExpressionInto(initExpr, dest.Value, context);
                    return;
                }

                using var srcReg = context.AcquireTempRegister();
                EmitExpression(initExprs[^1], srcReg.Value, context);

                using var destReg = context.AcquireTempRegister();
                context.Emit($"MOV r{destReg.Value}, r15");
                AccumulateOffset(destReg.Value, space.Value, context);

                if (sizeBytes <= 4)
                {
                    EmitInlineAggregateCopy(srcReg.Value, destReg.Value, (int)sizeBytes, context);
                }
                else
                {
                    context.Emit($"PUSH r{srcReg.Value}");
                    context.Emit($"MOV r1, r{destReg.Value}");
                    context.Emit("POP r2");
                    context.Emit($"LDI r3, {sizeBytes}");
                    context.Emit("CALL SYS_MEM_MOVE");
                }
            }
            return;
        }

        if (space.Slots > 1)
        {
            using var dstAddr = context.AcquireTempRegister();
            context.Emit($"MOV r{dstAddr.Value}, r15");
            AccumulateOffset(dstAddr.Value, space.Value, context);

            if (initExprs.Count == 0)
            {
                for (int s = 0; s < space.Slots; s++)
                {
                    using var zero = context.AcquireTempRegister();
                    context.Emit($"LDI r{zero.Value}, 0");
                    context.Emit($"STA r{zero.Value}, r{dstAddr.Value}");
                    if (s < space.Slots - 1)
                        context.Emit($"IADD r{dstAddr.Value}, 2");
                }
            }
            else
            {
                EmitLongToAddress(initExprs[^1], dstAddr.Value, context);
            }
            return;
        }

        using var valRegPrimitive = context.AcquireTempRegister();

        if (initExprs.Count == 0)
            context.Emit($"LDI r{valRegPrimitive.Value}, 0");
        else
            EmitExpression(initExprs[^1], valRegPrimitive.Value, context);

        if (space.Type == StorageType.Register)
        {
            context.Emit($"MOV r{space.Value}, r{valRegPrimitive.Value}");
            if (initExprs.Count > 0)
                TruncateToByteIfNeeded(space.Value, sizeBytes, initExprs[^1], context);
        }
        else
        {
            using var addrReg = context.AcquireTempRegister();
            context.Emit($"MOV r{addrReg.Value}, r15");
            AccumulateOffset(addrReg.Value, space.Value, context);

            string altPrefix = (sizeBytes == 1) ? "ALT " : "";
            context.Emit($"{altPrefix}STA r{valRegPrimitive.Value}, r{addrReg.Value}");
        }
    }
}
