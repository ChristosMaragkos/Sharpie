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

                EmitInlineAggregateCopy(srcAddrReg.Value, destAddrReg2.Value, (int)assignSize, context);
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

        if (
            kind
            is CXBinaryOperatorKind.CXBinaryOperator_ShlAssign
                or CXBinaryOperatorKind.CXBinaryOperator_ShrAssign
        )
        {
            using var shift = context.AcquireTempRegister();
            var shiftedPeeledRhs = PeelExpression(rhs);

            if (shiftedPeeledRhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
            {
                long sv = shiftedPeeledRhs.Evaluate.AsLongLong;
                context.Emit($"LDI r{shift.Value}, {unchecked((ushort)sv)}");
            }
            else
            {
                EmitExpression(rhs, shift.Value, context);
            }

            if (kind == CXBinaryOperatorKind.CXBinaryOperator_ShlAssign)
            {
                using var origLow = context.AcquireTempRegister();
                context.Emit($"MOV r{origLow.Value}, r{lhsLow.Value}");
                context.Emit($"SHL r{lhsLow.Value}, r{shift.Value}");
                context.Emit($"SHL r{lhsHigh.Value}, r{shift.Value}");
                context.Emit($"ALT SHL r{origLow.Value}, r{shift.Value}");
                context.Emit($"OR r{lhsHigh.Value}, r{origLow.Value}");
            }
            else
            {
                using var origHigh = context.AcquireTempRegister();
                context.Emit($"MOV r{origHigh.Value}, r{lhsHigh.Value}");
                context.Emit($"SHR r{lhsHigh.Value}, r{shift.Value}");
                context.Emit($"SHR r{lhsLow.Value}, r{shift.Value}");
                context.Emit($"ALT SHR r{origHigh.Value}, r{shift.Value}");
                context.Emit($"OR r{lhsLow.Value}, r{origHigh.Value}");
            }

            context.Emit($"STA r{lhsLow.Value}, r{addrReg.Value}");
            context.Emit($"IADD r{addrReg.Value}, 2");
            context.Emit($"STA r{lhsHigh.Value}, r{addrReg.Value}");
            return;
        }

        using var rhsLow = context.AcquireTempRegister();
        using var rhsHigh = context.AcquireTempRegister();
        var peeledRhs = PeelExpression(rhs);

        if (peeledRhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
        {
            long v = peeledRhs.Evaluate.AsLongLong;
            context.Emit($"LDI r{rhsLow.Value}, {unchecked((ushort)(v & 0xFFFF))}");
            context.Emit($"LDI r{rhsHigh.Value}, {unchecked((ushort)((v >> 16) & 0xFFFF))}");
        }
        else
        {
            using var src = context.AcquireTempRegister();
            EmitExpression(rhs, src.Value, context);
            context.Emit($"LDP r{rhsLow.Value}, r{src.Value}");
            context.Emit($"IADD r{src.Value}, 2");
            context.Emit($"LDP r{rhsHigh.Value}, r{src.Value}");
        }

        switch (kind)
        {
            case CXBinaryOperatorKind.CXBinaryOperator_AddAssign:
                context.Emit($"ADD r{lhsLow.Value}, r{rhsLow.Value}");
                context.Emit($"ALT ADD r{lhsHigh.Value}, r{rhsHigh.Value}");
                break;
            case CXBinaryOperatorKind.CXBinaryOperator_SubAssign:
                context.Emit($"SUB r{lhsLow.Value}, r{rhsLow.Value}");
                context.Emit($"ALT SUB r{lhsHigh.Value}, r{rhsHigh.Value}");
                break;
            case CXBinaryOperatorKind.CXBinaryOperator_AndAssign:
                context.Emit($"AND r{lhsLow.Value}, r{rhsLow.Value}");
                context.Emit($"AND r{lhsHigh.Value}, r{rhsHigh.Value}");
                break;
            case CXBinaryOperatorKind.CXBinaryOperator_OrAssign:
                context.Emit($"OR r{lhsLow.Value}, r{rhsLow.Value}");
                context.Emit($"OR r{lhsHigh.Value}, r{rhsHigh.Value}");
                break;
            case CXBinaryOperatorKind.CXBinaryOperator_XorAssign:
                context.Emit($"XOR r{lhsLow.Value}, r{rhsLow.Value}");
                context.Emit($"XOR r{lhsHigh.Value}, r{rhsHigh.Value}");
                break;
            case CXBinaryOperatorKind.CXBinaryOperator_MulAssign:
                {
                    using var origLow = context.AcquireTempRegister();
                    using var acc = context.AcquireTempRegister();
                    context.Emit($"MOV r{origLow.Value}, r{lhsLow.Value}");
                    context.Emit($"MUL r{lhsLow.Value}, r{rhsLow.Value}");
                    context.Emit($"MOV r{acc.Value}, r{origLow.Value}");
                    context.Emit($"ALT MUL r{acc.Value}, r{rhsLow.Value}");
                    context.Emit($"MUL r{origLow.Value}, r{rhsHigh.Value}");
                    context.Emit($"ADD r{acc.Value}, r{origLow.Value}");
                    context.Emit($"MOV r{origLow.Value}, r{lhsHigh.Value}");
                    context.Emit($"MUL r{origLow.Value}, r{rhsLow.Value}");
                    context.Emit($"ADD r{acc.Value}, r{origLow.Value}");
                    context.Emit($"MOV r{lhsHigh.Value}, r{acc.Value}");
                    break;
                }
            case CXBinaryOperatorKind.CXBinaryOperator_DivAssign:
            case CXBinaryOperatorKind.CXBinaryOperator_RemAssign:
                {
                    var tempLabel = EmissionContext.GenerateLabel("div_temp");
                    var tempSpace = context.AllocateStorage(tempLabel, true, 4);
                    using var bufAddr = context.AcquireTempRegister();
                    context.Emit($"MOV r{bufAddr.Value}, r15");
                    AccumulateOffset(bufAddr.Value, tempSpace.Value, context);

                    context.Emit($"PUSH r{addrReg.Value}");

                    context.Emit($"MOV r1, r{lhsHigh.Value}");
                    context.Emit($"MOV r2, r{lhsLow.Value}");

                    context.Emit($"MOV r3, r{rhsHigh.Value}");
                    context.Emit($"MOV r4, r{rhsLow.Value}");

                    int mode = kind == CXBinaryOperatorKind.CXBinaryOperator_DivAssign ? 0 : 1;
                    context.Emit($"LDI r0, {mode}");
                    context.Emit("PUSH r0");
                    context.Emit($"PUSH r{bufAddr.Value}");

                    context.Emit("CALL SYS_DIV_32");

                    context.Emit("POP r0");
                    context.Emit("POP r0");

                    context.Emit($"POP r{addrReg.Value}");

                    context.Emit($"MOV r{bufAddr.Value}, r15");
                    AccumulateOffset(bufAddr.Value, tempSpace.Value, context);

                    context.Emit($"LDP r{lhsLow.Value}, r{bufAddr.Value}");
                    context.Emit($"STA r{lhsLow.Value}, r{addrReg.Value}");
                    context.Emit($"IADD r{bufAddr.Value}, 2");
                    context.Emit($"IADD r{addrReg.Value}, 2");
                    context.Emit($"LDP r{lhsHigh.Value}, r{bufAddr.Value}");
                    context.Emit($"STA r{lhsHigh.Value}, r{addrReg.Value}");
                    break;
                }
            default:
                throw new InvalidOperationException($"Unsupported long compound assignment: {kind}");
        }

        if (kind != CXBinaryOperatorKind.CXBinaryOperator_DivAssign && kind != CXBinaryOperatorKind.CXBinaryOperator_RemAssign)
        {
            context.Emit($"STA r{lhsLow.Value}, r{addrReg.Value}");
            context.Emit($"IADD r{addrReg.Value}, 2");
            context.Emit($"STA r{lhsHigh.Value}, r{addrReg.Value}");
        }
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

                context.Emit($"PUSH r{srcReg.Value}");
                context.Emit($"MOV r1, r{destReg.Value}");
                context.Emit("POP r2");
                context.Emit($"LDI r3, {sizeBytes}");
                context.Emit("CALL SYS_MEM_MOVE");
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
