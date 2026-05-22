using ClangSharp.Interop;

namespace Sharpie.CCompiler;

public partial class SharpieEmitter
{
    private static void EmitStatement(CXCursor stmt, EmissionContext context)
    {
        switch (stmt.Kind)
        {
            case CXCursorKind.CXCursor_DeclStmt:
                EmitDeclarationStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_VarDecl:
                EmitVariableDeclaration(stmt, context);
                break;

            case CXCursorKind.CXCursor_BinaryOperator:
            case CXCursorKind.CXCursor_CompoundAssignOperator:
                EmitAssignmentStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_CallExpr:
                EmitCall(stmt, context);
                break;

            case CXCursorKind.CXCursor_ReturnStmt:
                EmitReturn(stmt, context);
                break;

            case CXCursorKind.CXCursor_IfStmt:
                EmitIfStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_WhileStmt:
                EmitWhileStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_ForStmt:
                EmitForStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_DoStmt:
                EmitDoStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_CompoundStmt:
                EmitFunctionBody(stmt, context);
                break;

            case CXCursorKind.CXCursor_UnaryOperator:
            case CXCursorKind.CXCursor_UnexposedExpr:
                EmitExpression(stmt, -1, context);
                break;

            case CXCursorKind.CXCursor_ContinueStmt:
                if (context.ContinueLabels.Count == 0)
                    throw new InvalidOperationException("Unexpected 'continue' outside of loop");
                context.Emit($"JMP {context.ContinueLabels.Peek()}");
                break;

            case CXCursorKind.CXCursor_BreakStmt:
                if (context.BreakLabels.Count == 0)
                {
                    throw new InvalidOperationException(
                        "Break statement outside of a loop or switch."
                    );
                }

                context.Emit($"JMP {context.BreakLabels.Peek()}");
                break;

            case CXCursorKind.CXCursor_CaseStmt:
                // A CaseStmt has two children: The constant value, and the statement to execute.
                // We will rely on EmitSwitchStmt to have generated the label for us, so we just emit the body
                var caseBody = GetChildren(stmt).Last();
                EmitStatement(caseBody, context);
                break;

            case CXCursorKind.CXCursor_DefaultStmt:
                // Same as CaseStmt
                var defaultBody = GetChildren(stmt).First();
                EmitStatement(defaultBody, context);
                break;

            case CXCursorKind.CXCursor_SwitchStmt:
                EmitSwitchStatement(stmt, context);
                break;

            case CXCursorKind.CXCursor_AsmStmt:
                ParseAndEmitAsmString(stmt, context.Emit);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported statement kind in `main`: {stmt.Kind}"
                );
        }
    }

    private static void EmitDeclarationStatement(CXCursor declStmt, EmissionContext context)
    {
        var declarations = GetChildren(declStmt);
        if (declarations.Count == 0)
            throw new InvalidOperationException("Declaration statement contained no declarations.");

        foreach (var declaration in declarations)
        {
            if (declaration.Kind != CXCursorKind.CXCursor_VarDecl)
            {
                throw new InvalidOperationException(
                    $"Unsupported declaration kind in statement: {declaration.Kind}"
                );
            }

            EmitVariableDeclaration(declaration, context);
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

                // better hope clang includes the null terminator
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

        // Identify actual inline initialization for arrays/structs
        var initList = GetChildren(varDecl)
            .FirstOrDefault(c => c.Kind == CXCursorKind.CXCursor_InitListExpr);
        var hasInitList = initList.Kind == CXCursorKind.CXCursor_InitListExpr;

        if (isRecord || isArray)
        {
            if (hasInitList)
            {
                var initVals = GetChildren(initList);

                // Get the base address of the array/struct
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
                        stride = 2; // Fallback

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
                        // Handle arrays of arrays (multidimensional arrays) by recursively
                        // unpacking the nested InitListExpr nodes.
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
                else // Structs
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

                // If it's a struct-returning call, write directly into the variable's stack home
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
            return; // done initializing
        }

        // Multi-word scalar init (e.g., long)
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

    private static void EmitAssignmentStatement(CXCursor assignmentCursor, EmissionContext context)
    {
        var children = GetChildren(assignmentCursor);
        if (children.Count != 2)
            throw new InvalidOperationException("Expected assignment to have exactly 2 operands.");

        var lhs = PeelExpression(children[0]);
        var rhs = PeelExpression(children[1]);

        // NEW: Intercept ALL compound assignments immediately, regardless of what they are targeting!
        if (assignmentCursor.Kind == CXCursorKind.CXCursor_CompoundAssignOperator)
        {
            EmitCompoundAssignment(assignmentCursor, lhs, rhs, context);
            return;
        }

        // Fast Path: Direct local variable in a register (r8-r15) or global variable
        if (lhs.Kind == CXCursorKind.CXCursor_DeclRefExpr)
        {
            var variableName = lhs.Spelling.ToString();

            // register
            if (
                context.Locals.TryGetValue(variableName, out var loc)
                && loc.Type == StorageType.Register
            )
            {
                EmitExpression(rhs, loc.Value, context);
                return;
            }
            // global
            else if (context.Globals.Contains(variableName))
            {
                var globalSize = lhs.Type.SizeOf;
                if (globalSize <= 2)
                {
                    using var valueReg = context.AcquireTempRegister();
                    EmitExpression(rhs, valueReg.Value, context);

                    var prefix = (globalSize == 1) ? "ALT " : "";
                    // Write directly to the label
                    context.Emit($"{prefix}STM r{valueReg.Value}, _global_{variableName}");
                    return;
                }
            }
        }

        var assignSize = lhs.Type.SizeOf;
        if (assignSize > 2)
        {
            // If RHS is a struct-returning call, write directly into destination (no temp buffers means no leaks)
            var peeledRhs = PeelExpression(rhs);
            if (peeledRhs.Kind == CXCursorKind.CXCursor_CallExpr && peeledRhs.Type.SizeOf > 2)
            {
                using var destAddrReg = context.AcquireTempRegister();
                EmitLValueAddress(lhs, destAddrReg.Value, context);

                EmitCallExpressionInto(peeledRhs, destAddrReg.Value, context);
                return;
            }

            // Compound literals are already field-wise expressions; writing them directly avoids
            // introducing temporary aggregate buffers and an additional copy step.
            if (peeledRhs.Kind == CXCursorKind.CXCursor_CompoundLiteralExpr)
            {
                using var destAddrReg = context.AcquireTempRegister();
                EmitLValueAddress(lhs, destAddrReg.Value, context);
                EmitCompoundLiteralIntoAddress(peeledRhs, destAddrReg.Value, context);
                return;
            }

            // For struct/record types, use the original aggregate copy pattern.
            // EmitExpression for a record returns the address; then we copy the full size.
            // For scalar 4-byte types (long), use EmitLongToAddress which handles
            // literals, math, and references correctly for multi-word values.
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

        // Memory assignment (for pointers, array indices, or stack locals)
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

        // 2. Perform the math
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

        // write back to stack (for locals, pointers, array indices, and struct members)
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
        // Load LHS address and both words
        using var addrReg = context.AcquireTempRegister();
        EmitLValueAddress(lhs, addrReg.Value, context);

        using var lhsLow = context.AcquireTempRegister();
        using var lhsHigh = context.AcquireTempRegister();
        context.Emit($"LDP r{lhsLow.Value}, r{addrReg.Value}");
        context.Emit($"IADD r{addrReg.Value}, 2");
        context.Emit($"LDP r{lhsHigh.Value}, r{addrReg.Value}");
        context.Emit($"IADD r{addrReg.Value}, -2"); // back to low word

        // Shift compound assignments use a single-register shift count RHS
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

        // Evaluate RHS into rhsLow/rhsHigh
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

        // Perform operation
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
                    // 32-bit multiply using partial products
                    // result_low = low(a_low * b_low)
                    // result_high = high(a_low * b_low) + low(a_low * b_high) + low(a_high * b_low)
                    using var origLow = context.AcquireTempRegister();
                    using var acc = context.AcquireTempRegister();
                    context.Emit($"MOV r{origLow.Value}, r{lhsLow.Value}");
                    context.Emit($"MUL r{lhsLow.Value}, r{rhsLow.Value}");       // lhsLow = low(a_low * b_low)
                    context.Emit($"MOV r{acc.Value}, r{origLow.Value}");         // acc = original a_low
                    context.Emit($"ALT MUL r{acc.Value}, r{rhsLow.Value}");      // acc = high(a_low * b_low)
                    context.Emit($"MUL r{origLow.Value}, r{rhsHigh.Value}");     // origLow = low(a_low * b_high)
                    context.Emit($"ADD r{acc.Value}, r{origLow.Value}");
                    context.Emit($"MOV r{origLow.Value}, r{lhsHigh.Value}");     // origLow = a_high
                    context.Emit($"MUL r{origLow.Value}, r{rhsLow.Value}");      // origLow = low(a_high * b_low)
                    context.Emit($"ADD r{acc.Value}, r{origLow.Value}");
                    context.Emit($"MOV r{lhsHigh.Value}, r{acc.Value}");         // lhsHigh = result_high
                    break;
                }
            default:
                throw new InvalidOperationException($"Unsupported long compound assignment: {kind}");
        }

        // Store result back
        context.Emit($"STA r{lhsLow.Value}, r{addrReg.Value}");
        context.Emit($"IADD r{addrReg.Value}, 2");
        context.Emit($"STA r{lhsHigh.Value}, r{addrReg.Value}");
    }

    private static void EmitReturn(CXCursor returnStmt, EmissionContext context)
    {
        var expr = GetChildren(returnStmt).FirstOrDefault();

        if (expr.Kind != CXCursorKind.CXCursor_NoDeclFound)
        {
            long retSizeBytes = expr.Type.SizeOf;

            // If returning a struct, mutate the hidden pointer copy
            if (retSizeBytes > 2 && context.HiddenRetPtrReg >= 0)
            {
                var peeled = PeelExpression(expr);

                if (peeled.Kind == CXCursorKind.CXCursor_CallExpr)
                {
                    EmitCallExpressionInto(peeled, context.HiddenRetPtrReg, context);
                }
                else if (retSizeBytes == 4)
                {
                    EmitLongToAddress(expr, context.HiddenRetPtrReg, context);
                }
                else
                {
                    using var srcReg = context.AcquireTempRegister();
                    EmitExpression(expr, srcReg.Value, context);

                    context.Emit($"PUSH r{srcReg.Value}");
                    context.Emit($"MOV r1, r{context.HiddenRetPtrReg}");
                    context.Emit("POP r2");
                    context.Emit($"LDI r3, {retSizeBytes}");
                    context.Emit("CALL SYS_MEM_MOVE");
                }
            }
            else // Normal 16-bit return
            {
                EmitExpression(expr, 0, context);
            }
        }

        context.Emit($"JMP {context.EpilogueLabel}");
        context.HasReturn = true;
    }

    private static void EmitExpression(CXCursor expr, int targetReg, EmissionContext context)
    {
        var node = PeelExpression(expr);

        var eval = node.Evaluate;
        if (eval.Kind == CXEvalResultKind.CXEval_Int)
        {
            context.Emit($"LDI r{targetReg}, {unchecked((ushort)eval.AsLongLong)}");
            return;
        }

        switch (node.Kind)
        {
            case 0:
                return;

            case CXCursorKind.CXCursor_StringLiteral:
                if (targetReg >= 0)
                {
                    var label = GetOrAddStringLiteral(node, context);
                    context.Emit($"LDI r{targetReg}, {label}");
                }
                return;

            case CXCursorKind.CXCursor_ConditionalOperator:
                if (targetReg >= 0)
                {
                    var condChildren = GetChildren(node);
                    var condExpr = PeelExpression(condChildren[0]);
                    var trueExpr = PeelExpression(condChildren[1]);
                    var falseExpr = PeelExpression(condChildren[2]);

                    var falseLabel = EmissionContext.GenerateLabel("ternary_false");
                    var endLabel = EmissionContext.GenerateLabel("ternary_end");

                    EmitCondition(condExpr, falseLabel, false, context);

                    EmitExpression(trueExpr, targetReg, context);
                    context.Emit($"JMP {endLabel}");

                    context.Emit($"{falseLabel}:");
                    EmitExpression(falseExpr, targetReg, context);

                    context.Emit($"{endLabel}:");
                }
                return;
            case CXCursorKind.CXCursor_CallExpr:
                EmitCallExpression(node, targetReg, context);
                return;

            case CXCursorKind.CXCursor_DeclRefExpr:
                var referenced = clang.getCursorReferenced(node);
                var name = node.Spelling.ToString();

                if (referenced.Kind == CXCursorKind.CXCursor_FunctionDecl)
                {
                    if (targetReg >= 0)
                        context.Emit($"LDI r{targetReg}, _func_{name}");

                    return;
                }

                if (context.Locals.TryGetValue(name, out var allocatedSpace))
                {
                    // Only load from the stack if someone is actually asking for the value
                    if (targetReg >= 0)
                    {
                        if (
                            node.Type.CanonicalType.kind
                            is CXTypeKind.CXType_ConstantArray
                                or CXTypeKind.CXType_Record
                            || allocatedSpace.Slots > 1
                        )
                        {
                            context.Emit($"MOV r{targetReg}, r15");
                            AccumulateOffset(targetReg, allocatedSpace.Value, context);
                            return;
                        }

                        var isByte = node.Type.SizeOf == 1;
                        var prefix = isByte ? "ALT " : "";

                        if (allocatedSpace.Type == StorageType.Stack)
                        {
                            using var addrReg = context.AcquireTempRegister();
                            context.Emit($"MOV r{addrReg.Value}, r15");
                            AccumulateOffset(addrReg.Value, allocatedSpace.Value, context);

                            context.Emit($"{prefix}LDP r{targetReg}, r{addrReg.Value}");
                        }
                        else if (targetReg != allocatedSpace.Value)
                        {
                            context.Emit($"MOV r{targetReg}, r{allocatedSpace.Value}");
                        }
                    }
                    return;
                }
                else if (context.Globals.Contains(name))
                {
                    if (targetReg >= 0)
                    {
                        if (
                            node.Type.CanonicalType.kind
                            is CXTypeKind.CXType_ConstantArray
                                or CXTypeKind.CXType_Record
                            || node.Type.SizeOf > 2
                        )
                        {
                            // Arrays still decay to pointers, so we must return a pointer to the first element
                            // Structs must also yield their address when evaluated as an expression so struct assignment can work like this:
                            // obj1 = obj2;
                            context.Emit($"LDI r{targetReg}, _global_{name}");
                            return;
                        }

                        // directly read from memory
                        var isByte = node.Type.SizeOf == 1;
                        var prefix = isByte ? "ALT " : "";
                        context.Emit($"{prefix}LDM r{targetReg}, _global_{name}");
                    }
                    return;
                }

                throw new InvalidOperationException($"Unknown local variable `{name}`.");

            case CXCursorKind.CXCursor_UnaryOperator:
                EmitUnaryExpression(node, targetReg, context);
                return;

            case CXCursorKind.CXCursor_BinaryOperator:
                EmitBinaryExpression(node, targetReg, context);
                return;

            case CXCursorKind.CXCursor_CompoundAssignOperator:
                var assignChildren = GetChildren(node);
                if (assignChildren.Count != 2)
                    throw new InvalidOperationException("Expected compound assignment to have exactly 2 operands.");

                var lhsAssign = PeelExpression(assignChildren[0]);
                var rhsAssign = PeelExpression(assignChildren[1]);

                EmitCompoundAssignment(node, lhsAssign, rhsAssign, context);

                if (targetReg >= 0)
                {
                    EmitExpression(lhsAssign, targetReg, context);
                }
                return;


            case CXCursorKind.CXCursor_MemberRefExpr:
                if (targetReg >= 0)
                {
                    if (TryEmitMemberReadFromStructReturnCall(node, targetReg, context))
                        return;

                    if (IsAggregateType(node.Type) || node.Type.SizeOf > 2)
                    {
                        EmitLValueAddress(node, targetReg, context);
                        return;
                    }

                    var isByte = node.Type.SizeOf == 1;
                    var prefix = isByte ? "ALT " : "";

                    using var addrReg = context.AcquireTempRegister();
                    EmitLValueAddress(node, addrReg.Value, context);
                    context.Emit($"{prefix}LDP r{targetReg}, r{addrReg.Value}");
                }
                return;
            case CXCursorKind.CXCursor_ArraySubscriptExpr:
                if (targetReg >= 0)
                {
                    if (IsAggregateType(node.Type) || node.Type.SizeOf > 2)
                    {
                        EmitLValueAddress(node, targetReg, context);
                        return;
                    }

                    var isByte = node.Type.SizeOf == 1;
                    var prefix = isByte ? "ALT " : "";

                    using var addrReg = context.AcquireTempRegister();
                    EmitLValueAddress(node, addrReg.Value, context);
                    context.Emit($"{prefix}LDP r{targetReg}, r{addrReg.Value}");
                }
                return;

            case CXCursorKind.CXCursor_UnexposedExpr:
                var children = GetChildren(node);
                if (children.Count > 0)
                {
                    // Recursively process the child
                    EmitExpression(children[0], targetReg, context);
                }
                else
                {
                    // If no children, it's an empty expression. 
                    // Emit a zero-value to prevent the register from being uninitialized.
                    if (targetReg >= 0)
                        context.Emit($"LDI r{targetReg}, 0");
                }
                return;

            case CXCursorKind.CXCursor_CompoundLiteralExpr:
                EmitCompoundLiteral(node, targetReg, context);
                return;
        }

        throw new InvalidOperationException($"Unsupported expression kind: {node.Kind}");
    }

    private static bool TryEmitMemberReadFromStructReturnCall(
        CXCursor node,
        int targetReg,
        EmissionContext context
    )
    {
        if (node.Kind != CXCursorKind.CXCursor_MemberRefExpr)
            return false;

        var children = GetChildren(node);
        if (children.Count == 0)
            return false;

        const int RegisterReturnByteWidth = 2;

        var baseExpr = PeelExpression(children[0]);
        if (
            baseExpr.Kind != CXCursorKind.CXCursor_CallExpr
            || baseExpr.Type.SizeOf <= RegisterReturnByteWidth
        )
        {
            return false;
        }

        var retSize = (int)baseExpr.Type.SizeOf;

        using var retAddrReg = context.AcquireTempRegister();
        EmitAllocStackframe(retSize, context);
        context.Emit($"MOV r{retAddrReg.Value}, r0");
        EmitCallExpressionInto(baseExpr, retAddrReg.Value, context);

        var fieldDecl = clang.getCursorReferenced(node);
        long offsetBits = clang.Cursor_getOffsetOfField(fieldDecl);
        if (offsetBits < 0)
        {
            throw new InvalidOperationException(
                $"Could not determine offset for struct field '{node.Spelling}'"
            );
        }
        if ((offsetBits % 8) != 0)
        {
            throw new InvalidOperationException(
                $"Unsupported non-byte-aligned struct field '{node.Spelling}' at bit offset {offsetBits}."
            );
        }

        long offsetBytes = offsetBits / 8;
        AccumulateOffset(retAddrReg.Value, (int)offsetBytes, context);

        var isByte = node.Type.SizeOf == 1;
        var prefix = isByte ? "ALT " : "";
        context.Emit($"{prefix}LDP r{targetReg}, r{retAddrReg.Value}");
        EmitFreeStackframe(retSize, context);

        return true;
    }

    private static void EmitUnaryExpression(
        CXCursor unaryExpr,
        int targetReg,
        EmissionContext context
    )
    {
        var operand = GetChildren(unaryExpr).FirstOrDefault();
        if (operand.Kind == CXCursorKind.CXCursor_NoDeclFound)
            throw new InvalidOperationException("Unary expression has no operand.");

        var unaryKind = GetUnaryOperatorKind(unaryExpr);
        var peeled = PeelExpression(operand);

        switch (unaryKind)
        {
            case CXUnaryOperatorKind.CXUnaryOperator_PreInc:
            case CXUnaryOperatorKind.CXUnaryOperator_PreDec:
            case CXUnaryOperatorKind.CXUnaryOperator_PostInc:
            case CXUnaryOperatorKind.CXUnaryOperator_PostDec:
                var isInc =
                    unaryKind
                    is CXUnaryOperatorKind.CXUnaryOperator_PreInc
                        or CXUnaryOperatorKind.CXUnaryOperator_PostInc;
                var isPost =
                    unaryKind
                    is CXUnaryOperatorKind.CXUnaryOperator_PostInc
                        or CXUnaryOperatorKind.CXUnaryOperator_PostDec;
                var op = isInc ? "INC" : "DEC";

                HandleIncDec(targetReg, context, peeled, isPost, op);

                return;

            case CXUnaryOperatorKind.CXUnaryOperator_AddrOf:
                EmitLValueAddress(peeled, targetReg, context);
                return;

            case CXUnaryOperatorKind.CXUnaryOperator_Deref:
                // For multi-word types (long, struct), return the address as-is (don't load)
                // The caller uses EmitLongToAddress / EmitInlineAggregateCopy to read the value
                EmitExpression(operand, targetReg, context);
                if (unaryExpr.Type.SizeOf <= 2)
                    context.Emit($"LDP r{targetReg}, r{targetReg}");
                return;

            case CXUnaryOperatorKind.CXUnaryOperator_Minus:
                if (operand.Type.SizeOf > 2)
                {
                    var tempSpace = context.AllocateStorage(EmissionContext.GenerateLabel("unary_temp"), true, (int)operand.Type.SizeOf);
                    using var addrReg = context.AcquireTempRegister();
                    context.Emit($"MOV r{addrReg.Value}, r15");
                    AccumulateOffset(addrReg.Value, tempSpace.Value, context);
                    EmitLongToAddress(unaryExpr, addrReg.Value, context);
                    if (targetReg >= 0)
                    {
                        context.Emit($"MOV r{targetReg}, r15");
                        AccumulateOffset(targetReg, tempSpace.Value, context);
                    }
                    return;
                }
                EmitExpression(operand, targetReg, context);
                context.Emit($"NEG r{targetReg}");
                return;

            case CXUnaryOperatorKind.CXUnaryOperator_Not:
                if (operand.Type.SizeOf > 2)
                {
                    var tempSpace = context.AllocateStorage(EmissionContext.GenerateLabel("unary_temp"), true, (int)operand.Type.SizeOf);
                    using var addrReg = context.AcquireTempRegister();
                    context.Emit($"MOV r{addrReg.Value}, r15");
                    AccumulateOffset(addrReg.Value, tempSpace.Value, context);
                    EmitLongToAddress(unaryExpr, addrReg.Value, context);
                    if (targetReg >= 0)
                    {
                        context.Emit($"MOV r{targetReg}, r15");
                        AccumulateOffset(targetReg, tempSpace.Value, context);
                    }
                    return;
                }
                EmitExpression(operand, targetReg, context);
                context.Emit($"NOT r{targetReg}");
                return;

            default:
                // Handle Plus (nop) or others
                EmitExpression(operand, targetReg, context);
                return;
        }
    }

    private static void HandleIncDec(
        int targetReg,
        EmissionContext context,
        CXCursor peeled,
        bool isPost,
        string op
    )
    {
        switch (peeled.Kind)
        {
            // 1. Handle Variables (Locals and Globals)
            case CXCursorKind.CXCursor_DeclRefExpr:
                var name = peeled.Spelling.ToString();

                if (context.Locals.TryGetValue(name, out var loc))
                {
                    // --- LOCAL VARIABLE ---
                    int mathReg;
                    EmissionContext.TempLease valRegLease;
                    EmissionContext.TempLease addrLease;
                    if (loc.Type == StorageType.Register)
                    {
                        mathReg = loc.Value;

                        if (isPost)
                        {
                            if (targetReg >= 0)
                                context.Emit($"MOV r{targetReg}, r{mathReg}");
                            context.Emit($"{op} r{mathReg}");
                        }
                        else
                        {
                            context.Emit($"{op} r{mathReg}");
                            if (targetReg >= 0)
                                context.Emit($"MOV r{targetReg}, r{mathReg}");
                        }
                    }
                    else if (loc.Slots > 1)
                    {
                        valRegLease = context.AcquireTempRegister();
                        addrLease = context.AcquireTempRegister();
                        mathReg = valRegLease.Value;

                        using var highReg = context.AcquireTempRegister();
                        int addrReg = addrLease.Value;
                        int highR = highReg.Value;

                        context.Emit($"MOV r{addrReg}, r15");
                        AccumulateOffset(addrReg, loc.Value, context);

                        context.Emit($"LDP r{mathReg}, r{addrReg}");
                        context.Emit($"IADD r{addrReg}, 2");
                        context.Emit($"LDP r{highR}, r{addrReg}");

                        if (isPost && targetReg >= 0)
                        {
                            context.Emit($"MOV r{targetReg}, r{mathReg}");
                        }

                        context.Emit($"{op} r{mathReg}");
                        string altOp = op == "INC" ? "ALT ADD" : "ALT SUB";
                        context.Emit($"{altOp} r{highR}, 0");

                        context.Emit($"STA r{highR}, r{addrReg}");
                        context.Emit($"IADD r{addrReg}, -2");
                        context.Emit($"STA r{mathReg}, r{addrReg}");

                        valRegLease.Dispose();
                        addrLease.Dispose();
                    }
                    else
                    {
                        valRegLease = context.AcquireTempRegister();
                        addrLease = context.AcquireTempRegister();
                        mathReg = valRegLease.Value;

                        context.Emit($"MOV r{addrLease.Value}, r15");
                        AccumulateOffset(addrLease.Value, loc.Value, context);
                        context.Emit($"LDP r{mathReg}, r{addrLease.Value}");

                        if (isPost)
                        {
                            if (targetReg >= 0)
                                context.Emit($"MOV r{targetReg}, r{mathReg}");
                            context.Emit($"{op} r{mathReg}");
                        }
                        else
                        {
                            context.Emit($"{op} r{mathReg}");
                            if (targetReg >= 0)
                                context.Emit($"MOV r{targetReg}, r{mathReg}");
                        }

                        context.Emit($"STA r{mathReg}, r{addrLease.Value}");
                        valRegLease.Dispose();
                        addrLease.Dispose();
                    }
                }
                else if (context.Globals.Contains(name))
                {
                    // --- GLOBAL VARIABLE ---
                    var globalSize = peeled.Type.SizeOf;

                    if (globalSize > 2)
                    {
                        using var addrReg = context.AcquireTempRegister();
                        using var lowReg = context.AcquireTempRegister();
                        using var highReg = context.AcquireTempRegister();
                        int addr = addrReg.Value;

                        context.Emit($"LDI r{addr}, _global_{name}");

                        context.Emit($"LDP r{lowReg.Value}, r{addr}");
                        context.Emit($"IADD r{addr}, 2");
                        context.Emit($"LDP r{highReg.Value}, r{addr}");

                        if (isPost && targetReg >= 0)
                        {
                            context.Emit($"MOV r{targetReg}, r{lowReg.Value}");
                        }

                        context.Emit($"{op} r{lowReg.Value}");
                        string altOp = op == "INC" ? "ALT ADD" : "ALT SUB";
                        context.Emit($"{altOp} r{highReg.Value}, 0");

                        context.Emit($"STA r{highReg.Value}, r{addr}");
                        context.Emit($"IADD r{addr}, -2");
                        context.Emit($"STA r{lowReg.Value}, r{addr}");
                    }
                    else
                    {
                        using var valRegLease = context.AcquireTempRegister();
                        int mathReg = valRegLease.Value;

                        var isByte = globalSize == 1;
                        var prefix = isByte ? "ALT " : "";

                        // Load absolute
                        context.Emit($"{prefix}LDM r{mathReg}, _global_{name}");

                        // Math
                        if (isPost)
                        {
                            if (targetReg >= 0)
                                context.Emit($"MOV r{targetReg}, r{mathReg}");
                            context.Emit($"{op} r{mathReg}");
                        }
                        else
                        {
                            context.Emit($"{op} r{mathReg}");
                            if (targetReg >= 0)
                                context.Emit($"MOV r{targetReg}, r{mathReg}");
                        }

                        // Store absolute (The Peephole Optimizer will crush this sequence!)
                        context.Emit($"{prefix}STM r{mathReg}, _global_{name}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Unknown variable {name}");
                }
                break;

            // 2. Handle Pointer Dereference (*ptr)++
            case CXCursorKind.CXCursor_UnaryOperator
                when GetUnaryOperatorKind(peeled) == CXUnaryOperatorKind.CXUnaryOperator_Deref:
                {
                    using var addrReg = context.AcquireTempRegister();
                    var ptrExpr = GetChildren(peeled).First();
                    EmitExpression(ptrExpr, addrReg.Value, context);

                    using var valReg = context.AcquireTempRegister();

                    context.Emit($"LDP r{valReg.Value}, r{addrReg.Value}");

                    if (isPost)
                    {
                        if (targetReg >= 0)
                            context.Emit($"MOV r{targetReg}, r{valReg.Value}");
                        context.Emit($"{op} r{valReg.Value}");
                    }
                    else
                    {
                        context.Emit($"{op} r{valReg.Value}");
                        if (targetReg >= 0)
                            context.Emit($"MOV r{targetReg}, r{valReg.Value}");
                    }

                    context.Emit($"STA r{valReg.Value}, r{addrReg.Value}");
                    break;
                }
        }
    }

    private static void EmitBinaryExpression(
        CXCursor binaryExpr,
        int targetReg,
        EmissionContext context
    )
    {
        var kind = GetBinaryOperatorKind(binaryExpr);

        if (
            kind
            is CXBinaryOperatorKind.CXBinaryOperator_LAnd
                or CXBinaryOperatorKind.CXBinaryOperator_LOr
        )
        {
            EmitLogicalOperator(binaryExpr, kind, targetReg, context);
            return;
        }

        if (kind == CXBinaryOperatorKind.CXBinaryOperator_Assign)
            throw new InvalidOperationException("Assignment is only supported as a statement.");

        var operands = GetChildren(binaryExpr);
        if (operands.Count != 2)
        {
            throw new InvalidOperationException(
                "Binary expression must have exactly two operands."
            );
        }

        var lhs = PeelExpression(operands[0]);
        var rhs = PeelExpression(operands[1]);

        bool needsFallback = targetReg < TempRegisterStart || targetReg > TempRegisterEnd;
        EmissionContext.TempLease fallbackLease = default;
        var outReg = targetReg;

        if (needsFallback)
        {
            fallbackLease = context.AcquireTempRegister();
            outReg = fallbackLease.Value;
        }

        // NEW: Evaluate Relational Operators into a Boolean 1 or 0
        if (
            kind
            is CXBinaryOperatorKind.CXBinaryOperator_EQ
                or CXBinaryOperatorKind.CXBinaryOperator_NE
                or CXBinaryOperatorKind.CXBinaryOperator_LT
                or CXBinaryOperatorKind.CXBinaryOperator_GT
                or CXBinaryOperatorKind.CXBinaryOperator_LE
                or CXBinaryOperatorKind.CXBinaryOperator_GE
        )
        {
            var trueLabel = EmissionContext.GenerateLabel("rel_true");
            var endLabel = EmissionContext.GenerateLabel("rel_end");

            using var leftReg = context.AcquireTempRegister();
            EmitExpression(lhs, leftReg.Value, context);

            if (TryGetByteLiteral(rhs, out var immValue))
            {
                context.Emit($"ICMP r{leftReg.Value}, {immValue}");
            }
            else
            {
                using var rightReg = context.AcquireTempRegister();
                EmitExpression(rhs, rightReg.Value, context);
                context.Emit($"CMP r{leftReg.Value}, r{rightReg.Value}");
            }

            var jumpMnemonic = GetJumpMnemonic(kind, true);
            context.Emit($"{jumpMnemonic} {trueLabel}");
            context.Emit($"LDI r{outReg}, 0"); // False
            context.Emit($"JMP {endLabel}");
            context.Emit($"{trueLabel}:");
            context.Emit($"LDI r{outReg}, 1"); // True
            context.Emit($"{endLabel}:");

            if (targetReg >= 0 && targetReg != outReg)
                context.Emit($"MOV r{targetReg}, r{outReg}");

            if (needsFallback)
                fallbackLease.Dispose();

            return;
        }

        // Multi-word expressions: route through EmitLongToAddress into a temp buffer
        if (binaryExpr.Type.SizeOf > 2)
        {
            var tempSpace = context.AllocateStorage(EmissionContext.GenerateLabel("bin_temp"), true, (int)binaryExpr.Type.SizeOf);
            using var addrReg = context.AcquireTempRegister();
            context.Emit($"MOV r{addrReg.Value}, r15");
            AccumulateOffset(addrReg.Value, tempSpace.Value, context);
            EmitLongToAddress(binaryExpr, addrReg.Value, context);

            if (targetReg >= 0)
            {
                context.Emit($"MOV r{targetReg}, r15");
                AccumulateOffset(targetReg, tempSpace.Value, context);
            }

            if (needsFallback)
                fallbackLease.Dispose();

            return;
        }

        // Standard Math Operators
        EmitExpression(lhs, outReg, context);

        if (!TryEmitImmediateMath(kind, outReg, rhs, context))
        {
            using var rhsScratch = context.AcquireTempRegister();
            EmitExpression(rhs, rhsScratch.Value, context);

            var op = kind switch
            {
                CXBinaryOperatorKind.CXBinaryOperator_Add => "ADD",
                CXBinaryOperatorKind.CXBinaryOperator_Sub => "SUB",
                CXBinaryOperatorKind.CXBinaryOperator_Mul => "MUL",
                CXBinaryOperatorKind.CXBinaryOperator_Div => "DIV",
                CXBinaryOperatorKind.CXBinaryOperator_Rem => "MOD",
                CXBinaryOperatorKind.CXBinaryOperator_And => "AND",
                CXBinaryOperatorKind.CXBinaryOperator_Or => "OR",
                CXBinaryOperatorKind.CXBinaryOperator_Xor => "XOR",
                CXBinaryOperatorKind.CXBinaryOperator_Shl => "SHL",
                CXBinaryOperatorKind.CXBinaryOperator_Shr => "SHR",
                _ => throw new InvalidOperationException($"Unsupported binary operator: {kind}"),
            };

            context.Emit($"{op} r{outReg}, r{rhsScratch.Value}");
        }

        if (targetReg >= 0 && targetReg != outReg)
            context.Emit($"MOV r{targetReg}, r{outReg}");

        if (needsFallback)
            fallbackLease.Dispose();
    }

    private static void EmitLogicalOperator(
        CXCursor binaryExpr,
        CXBinaryOperatorKind kind,
        int targetReg,
        EmissionContext context
    )
    {
        var operands = GetChildren(binaryExpr);
        var lhs = PeelExpression(operands[0]);
        var rhs = PeelExpression(operands[1]);

        bool needsFallback = targetReg < TempRegisterStart || targetReg > TempRegisterEnd;
        EmissionContext.TempLease fallbackLease = default;
        var outReg = targetReg;

        if (needsFallback)
        {
            fallbackLease = context.AcquireTempRegister();
            outReg = fallbackLease.Value;
        }

        var trueLabel = EmissionContext.GenerateLabel("logical_true");
        var falseLabel = EmissionContext.GenerateLabel("logical_false");
        var endLabel = EmissionContext.GenerateLabel("logical_end");

        if (kind == CXBinaryOperatorKind.CXBinaryOperator_LAnd) // &&
        {
            EmitExpression(lhs, outReg, context);
            context.Emit($"ICMP r{outReg}, 0");
            context.Emit($"JEQ {falseLabel}"); // Left is 0? Short-circuit to false!

            EmitExpression(rhs, outReg, context);
            context.Emit($"ICMP r{outReg}, 0");
            context.Emit($"JEQ {falseLabel}"); // Right is 0? Fail to false.

            context.Emit($"LDI r{outReg}, 1"); // Both are true
            context.Emit($"JMP {endLabel}");

            context.Emit($"{falseLabel}:");
            context.Emit($"LDI r{outReg}, 0");
            context.Emit($"{endLabel}:");
        }
        else // ||
        {
            EmitExpression(lhs, outReg, context);
            context.Emit($"ICMP r{outReg}, 0");
            context.Emit($"JNE {trueLabel}"); // Left is 1? Short-circuit to true!

            EmitExpression(rhs, outReg, context);
            context.Emit($"ICMP r{outReg}, 0");
            context.Emit($"JNE {trueLabel}"); // Right is 1? Succeed to true.

            context.Emit($"LDI r{outReg}, 0"); // Both are false
            context.Emit($"JMP {endLabel}");

            context.Emit($"{trueLabel}:");
            context.Emit($"LDI r{outReg}, 1");
            context.Emit($"{endLabel}:");
        }

        if (targetReg >= 0 && targetReg != outReg)
            context.Emit($"MOV r{targetReg}, r{outReg}");

        if (needsFallback)
            fallbackLease.Dispose();
    }

    private static void EmitIfStatement(CXCursor ifStatement, EmissionContext context)
    {
        var children = GetChildren(ifStatement);

        var condition = children[0];
        var thenBranch = children[1];
        var hasElse = children.Count > 2;

        var labelEnd = EmissionContext.GenerateLabel("if");
        var labelElse = hasElse ? EmissionContext.GenerateLabel("else") : labelEnd;

        EmitCondition(condition, labelElse, false, context);

        EmitStatement(thenBranch, context);

        if (hasElse)
        {
            if (!context.HasReturn)
                context.Emit($"JMP {labelEnd}");

            context.Emit($"{labelElse}:");
            context.HasReturn = false;
            EmitStatement(children[2], context);
        }

        context.Emit($"{labelEnd}:");
    }

    private static void EmitCondition(
        CXCursor condition,
        string targetLabel,
        bool jumpIfTrue,
        EmissionContext context
    )
    {
        var node = PeelExpression(condition);

        if (node.Kind == CXCursorKind.CXCursor_BinaryOperator)
        {
            var kind = GetBinaryOperatorKind(node);

            // Let && and || fall through to the short-circuiting logic in EmitExpression
            if (
                kind
                is not (
                    CXBinaryOperatorKind.CXBinaryOperator_LAnd
                    or CXBinaryOperatorKind.CXBinaryOperator_LOr
                )
            )
            {
                var operands = GetChildren(node);
                var lhs = PeelExpression(operands[0]);
                var rhs = PeelExpression(operands[1]);

                // 32-bit comparison (both operands are long)
                if (lhs.Type.SizeOf > 2 || rhs.Type.SizeOf > 2)
                {
                    using var lhsBase = context.AcquireTempRegister();
                    using var rhsBase = context.AcquireTempRegister();
                    using var aLow = context.AcquireTempRegister();
                    using var aHigh = context.AcquireTempRegister();
                    using var bLow = context.AcquireTempRegister();
                    using var bHigh = context.AcquireTempRegister();

                    EmitExpression(lhs, lhsBase.Value, context);
                    EmitExpression(rhs, rhsBase.Value, context);

                    // Load both words of A
                    context.Emit($"LDP r{aLow.Value}, r{lhsBase.Value}");
                    context.Emit($"IADD r{lhsBase.Value}, 2");
                    context.Emit($"LDP r{aHigh.Value}, r{lhsBase.Value}");

                    // Load both words of B
                    context.Emit($"LDP r{bLow.Value}, r{rhsBase.Value}");
                    context.Emit($"IADD r{rhsBase.Value}, 2");
                    context.Emit($"LDP r{bHigh.Value}, r{rhsBase.Value}");

                    var labelCmpDone = EmissionContext.GenerateLabel("cmp_done");

                    // Compare high words (signed)
                    context.Emit($"CMP r{aHigh.Value}, r{bHigh.Value}");
                    context.Emit($"JNE {labelCmpDone}");

                    // High words equal: compare low words using XOR 0x8000 trick for unsigned
                    context.Emit($"LDI r{lhsBase.Value}, 0x8000");
                    context.Emit($"XOR r{aLow.Value}, r{lhsBase.Value}");
                    context.Emit($"XOR r{bLow.Value}, r{lhsBase.Value}");
                    context.Emit($"CMP r{aLow.Value}, r{bLow.Value}");

                    context.Emit($"{labelCmpDone}:");
                    var jumpMnemonic = GetJumpMnemonic(kind, jumpIfTrue);
                    context.Emit($"{jumpMnemonic} {targetLabel}");

                    return;
                }

                using var leftReg = context.AcquireTempRegister();
                EmitExpression(lhs, leftReg.Value, context);

                if (TryGetByteLiteral(rhs, out var immValue))
                {
                    context.Emit($"ICMP r{leftReg.Value}, {immValue}");
                }
                else
                {
                    using var rightReg = context.AcquireTempRegister();
                    EmitExpression(rhs, rightReg.Value, context);
                    context.Emit($"CMP r{leftReg.Value}, r{rightReg.Value}");
                }

                var jumpMnemonic16 = GetJumpMnemonic(kind, jumpIfTrue);
                context.Emit($"{jumpMnemonic16} {targetLabel}");
                return;
            }
        }

        // Multi-word general expression (e.g., if (x) where x is long)
        if (node.Type.SizeOf > 2)
        {
            using var addr = context.AcquireTempRegister();
            using var low = context.AcquireTempRegister();
            using var high = context.AcquireTempRegister();
            EmitExpression(node, addr.Value, context);
            context.Emit($"LDP r{low.Value}, r{addr.Value}");
            context.Emit($"IADD r{addr.Value}, 2");
            context.Emit($"LDP r{high.Value}, r{addr.Value}");
            context.Emit($"OR r{low.Value}, r{high.Value}");
            context.Emit($"ICMP r{low.Value}, 0");
            context.Emit(jumpIfTrue ? $"JNE {targetLabel}" : $"JEQ {targetLabel}");
            return;
        }

        // variables, function calls, and logical operators (&&, ||)
        using var reg = context.AcquireTempRegister();
        EmitExpression(node, reg.Value, context);
        context.Emit($"ICMP r{reg.Value}, 0");
        context.Emit(jumpIfTrue ? $"JNE {targetLabel}" : $"JEQ {targetLabel}");
    }

    private static string GetJumpMnemonic(CXBinaryOperatorKind kind, bool jumpIfTrue)
    {
        return kind switch
        {
            CXBinaryOperatorKind.CXBinaryOperator_EQ => jumpIfTrue ? "JEQ" : "JNE",
            CXBinaryOperatorKind.CXBinaryOperator_NE => jumpIfTrue ? "JNE" : "JEQ",
            CXBinaryOperatorKind.CXBinaryOperator_LT => jumpIfTrue ? "JLT" : "JGE",
            CXBinaryOperatorKind.CXBinaryOperator_GT => jumpIfTrue ? "JGT" : "JLE",
            CXBinaryOperatorKind.CXBinaryOperator_LE => jumpIfTrue ? "JLE" : "JGT",
            CXBinaryOperatorKind.CXBinaryOperator_GE => jumpIfTrue ? "JGE" : "JLT",
            _ => throw new InvalidOperationException($"Unsupported comparison operator: {kind}"),
        };
    }

    private static void EmitCall(CXCursor callExpr, EmissionContext context)
    {
        EmitCallExpression(callExpr, -1, context);
    }

    private static void EmitCallExpression(
        CXCursor callExpr,
        int targetReg,
        EmissionContext context
    )
    {
        var children = GetChildren(callExpr);
        var callee = PeelExpression(children[0]);

        var referenced = clang.getCursorReferenced(callee);
        bool isDirectCall = referenced.Kind == CXCursorKind.CXCursor_FunctionDecl;

        var funcName = isDirectCall ? children[0].Spelling.ToString() : "";

        long retSize = callExpr.Type.SizeOf;
        bool hasHiddenPtr = retSize > 2;
        bool needsDiscardSRetBuffer = hasHiddenPtr && targetReg < 0;
        bool unsupportedAggregateRValue = hasHiddenPtr && targetReg >= 0;

        if (unsupportedAggregateRValue)
        {
            throw new InvalidOperationException(
                "Struct-return call expressions cannot be used as rvalues directly. "
                    + "Use variable initialization or aggregate assignment so the call can emit directly into a destination."
            );
        }

        var activeLeases = new List<EmissionContext.TempLease>();
        int indirectTargetSpillOffset = -1;

        if (!isDirectCall)
        {
            using var calleeRegLease = context.AcquireTempRegister();
            EmitExpression(callee, calleeRegLease.Value, context);

            // Securely allocate 2 bytes on the local stack to hold the pointer
            var loc = context.AllocateStorage("__hidden_indirect", true, 2);
            context.Emit("MOV r6, r15");
            AccumulateOffset(6, loc.Value, context);
            context.Emit($"STA r{calleeRegLease.Value}, r6");

            indirectTargetSpillOffset = loc.Value;
        }

        var tempsToProtect = context.GetActiveTempRegisters();
        foreach (var reg in tempsToProtect)
        {
            int offset = context.GetSpillOffset(reg);
            context.Emit("MOV r0, r15");
            AccumulateOffset(0, offset, context);
            context.Emit($"STA r{reg}, r0");
        }

        var regArgs = new List<(CXCursor Expr, int Slots)>();
        var stackArgs = new List<(CXCursor Expr, int Slots)>();

        // Shift starting register if we have a hidden pointer since it becomes the first argument
        int currentReg = hasHiddenPtr ? 2 : 1;

        for (int i = 1; i < children.Count; i++)
        {
            var arg = children[i];
            int slots = GetRegistersNeededForVariable(arg.Type);

            if (currentReg + slots - 1 <= 4)
            {
                regArgs.Add((arg, slots));
                currentReg += slots;
            }
            else
            {
                stackArgs.Add((arg, slots));
            }
        }

        // process stack arguments right to left
        int totalStackBytesToFree = 0;
        for (int i = stackArgs.Count - 1; i >= 0; i--)
        {
            var (arg, slots) = stackArgs[i];
            totalStackBytesToFree += slots * 2;

            if (slots == 1)
            {
                using var lease = context.AcquireTempRegister();
                EmitExpression(arg, lease.Value, context);
                context.Emit($"PUSH r{lease.Value}");
            }
            else
            {
                using var addrReg = context.AcquireTempRegister();
                EmitLValueAddress(arg, addrReg.Value, context);

                if (addrReg.Value == 2)
                    context.Emit("MOV r1, r2");
                else if (addrReg.Value != 1)
                    context.Emit($"MOV r1, r{addrReg.Value}");

                context.Emit($"LDI r2, {slots * 2}");
                context.Emit("CALL SYS_STACKALLOC");
            }
        }

        // process register arguments left to right
        var regAssignments = new List<(int TargetReg, int SourceTempReg)>();

        int abiReg = hasHiddenPtr ? 2 : 1;

        foreach (var (arg, slots) in regArgs)
        {
            if (slots == 1)
            {
                var lease = context.AcquireTempRegister();
                activeLeases.Add(lease);
                EmitExpression(arg, lease.Value, context);
                regAssignments.Add((abiReg, lease.Value));
                abiReg++;
            }
            else
            {
                using var addrReg = context.AcquireTempRegister();
                EmitLValueAddress(arg, addrReg.Value, context);

                for (int s = 0; s < slots; s++)
                {
                    var lease = context.AcquireTempRegister();
                    activeLeases.Add(lease);

                    context.Emit($"LDP r{lease.Value}, r{addrReg.Value}");
                    if (s < slots - 1)
                        context.Emit($"IADD r{addrReg.Value}, 2");

                    regAssignments.Add((abiReg, lease.Value));
                    abiReg++;
                }
            }
        }

        foreach (var (targetRegAbi, sourceTempReg) in regAssignments)
        {
            if (targetRegAbi != sourceTempReg)
                context.Emit($"MOV r{targetRegAbi}, r{sourceTempReg}");
        }

        if (needsDiscardSRetBuffer)
        {
            EmitAllocStackframe((int)retSize, context);
            context.Emit("MOV r1, r0");
        }

        if (!TryEmitIntrinsic(funcName, context))
        {
            // HACK: Cross bank calls:
            // To call a method in another bank, you need to annotate it like so:
            // __attribute__((annotate("bank_2"))) void foo(void);

            string? targetBank = null;

            unsafe
            {
                referenced.VisitChildren(
                    (child, _, _) =>
                    {
                        if (child.Kind == CXCursorKind.CXCursor_AnnotateAttr)
                        {
                            var annotation = child.Spelling.ToString();
                            if (annotation.StartsWith("bank_"))
                                targetBank = annotation.Substring(5);
                        }
                        return CXChildVisitResult.CXChildVisit_Continue;
                    },
                    new CXClientData(IntPtr.Zero)
                );
            }

            if (targetBank != null)
            {
                context.Emit("PUSH r13");
                context.Emit("PUSH r14");
                context.Emit($"LDI r14, {targetBank}");

                if (isDirectCall)
                {
                    context.Emit($"LDI r13, _func_{funcName}");
                }
                else
                {
                    context.Emit("MOV r6, r15");
                    AccumulateOffset(6, indirectTargetSpillOffset, context);
                    context.Emit("LDP r13, r6");
                }

                context.Emit("CALL SYS_FAR_CALL"); // the BIOS calls are always mapped so we can rugpull safely
                context.Emit("POP r14");
                context.Emit("POP r13");
            }
            else if (isDirectCall)
            {
                context.Emit($"CALL _func_{funcName}");
            }
            else
            {
                context.Emit("MOV r6, r15");
                AccumulateOffset(6, indirectTargetSpillOffset, context);
                context.Emit("LDP r0, r6");
                context.Emit("ALT CALL r0");
            }
            if (totalStackBytesToFree > 0)
            {
                context.Emit($"LDI r1, {totalStackBytesToFree}");
                context.Emit("CALL SYS_FREE_STACKFRAME");
            }
        }

        if (needsDiscardSRetBuffer)
            EmitFreeStackframe((int)retSize, context);

        if (tempsToProtect.Count > 0)
        {
            context.Emit("PUSH r0");

            foreach (var reg in tempsToProtect)
            {
                int offset = context.GetSpillOffset(reg);
                context.Emit("MOV r0, r15");
                AccumulateOffset(0, offset, context);
                context.Emit($"LDP r{reg}, r0");
            }

            context.Emit("POP r0");
        }

        if (targetReg >= 0)
        {
            if (hasHiddenPtr)
            {
                if (targetReg != 1)
                    context.Emit($"MOV r{targetReg}, r1");
            }
            else if (targetReg != 0)
            {
                context.Emit($"MOV r{targetReg}, r0");
            }
        }

        foreach (var lease in activeLeases)
            lease.Dispose();
    }

    private static void EmitCallExpressionInto(
    CXCursor callExpr,
    int destAddrReg,
    EmissionContext context
)
    {
        var children = GetChildren(callExpr);
        var callee = PeelExpression(children[0]);

        var referenced = clang.getCursorReferenced(callee);
        bool isDirectCall = referenced.Kind == CXCursorKind.CXCursor_FunctionDecl;
        var funcName = isDirectCall ? children[0].Spelling.ToString() : "";

        long retSize = callExpr.Type.SizeOf;
        if (retSize <= 2)
            throw new InvalidOperationException("EmitCallExpressionInto called for non-aggregate return.");

        int indirectTargetSpillOffset = -1;

        if (!isDirectCall)
        {
            using var calleeRegLease = context.AcquireTempRegister();
            EmitExpression(callee, calleeRegLease.Value, context);

            var loc = context.AllocateStorage("__hidden_indirect", true, 2);
            context.Emit("MOV r6, r15");
            AccumulateOffset(6, loc.Value, context);
            context.Emit($"STA r{calleeRegLease.Value}, r6");
            indirectTargetSpillOffset = loc.Value;
        }

        var tempsToProtect = context.GetActiveTempRegisters();
        foreach (var reg in tempsToProtect)
        {
            int offset = context.GetSpillOffset(reg);
            context.Emit("MOV r0, r15");
            AccumulateOffset(0, offset, context);
            context.Emit($"STA r{reg}, r0");
        }

        // Partition args into reg/stack, with sret for r1
        var regArgs = new List<(CXCursor Expr, int Slots)>();
        var stackArgs = new List<(CXCursor Expr, int Slots)>();

        int currentReg = 2;

        for (int i = 1; i < children.Count; i++)
        {
            var arg = children[i];
            int slots = GetRegistersNeededForVariable(arg.Type);

            if (currentReg + slots - 1 <= 4)
            {
                regArgs.Add((arg, slots));
                currentReg += slots;
            }
            else
            {
                stackArgs.Add((arg, slots));
            }
        }

        // stack args right to left
        int totalStackBytesToFree = 0;
        for (int i = stackArgs.Count - 1; i >= 0; i--)
        {
            var (arg, slots) = stackArgs[i];
            totalStackBytesToFree += slots * 2;

            if (slots == 1)
            {
                using var lease = context.AcquireTempRegister();
                EmitExpression(arg, lease.Value, context);
                context.Emit($"PUSH r{lease.Value}");
            }
            else
            {
                using var addrReg = context.AcquireTempRegister();
                EmitLValueAddress(arg, addrReg.Value, context);

                if (addrReg.Value == 2)
                    context.Emit("MOV r1, r2");
                else if (addrReg.Value != 1)
                    context.Emit($"MOV r1, r{addrReg.Value}");

                context.Emit($"LDI r2, {slots * 2}");
                context.Emit("CALL SYS_STACKALLOC");
            }
        }

        // reg args left-to-right
        var activeLeases = new List<EmissionContext.TempLease>();
        var regAssignments = new List<(int TargetReg, int SourceTempReg)>();

        int abiReg = 2;

        foreach (var (arg, slots) in regArgs)
        {
            if (slots == 1)
            {
                var lease = context.AcquireTempRegister();
                activeLeases.Add(lease);
                EmitExpression(arg, lease.Value, context);
                regAssignments.Add((abiReg, lease.Value));
                abiReg++;
            }
            else
            {
                using var addrReg = context.AcquireTempRegister();
                EmitLValueAddress(arg, addrReg.Value, context);

                for (int s = 0; s < slots; s++)
                {
                    var lease = context.AcquireTempRegister();
                    activeLeases.Add(lease);

                    context.Emit($"LDP r{lease.Value}, r{addrReg.Value}");
                    if (s < slots - 1)
                        context.Emit($"IADD r{addrReg.Value}, 2");

                    regAssignments.Add((abiReg, lease.Value));
                    abiReg++;
                }
            }
        }

        foreach (var (target, src) in regAssignments)
        {
            if (target != src)
                context.Emit($"MOV r{target}, r{src}");
        }

        // Set hidden sret pointer: r1 = destAddrReg
        if (destAddrReg != 1)
            context.Emit($"MOV r1, r{destAddrReg}");

        // Call (intrinsic or normal)
        if (!TryEmitIntrinsic(funcName, context))
        {
            string? targetBank = null;
            unsafe
            {
                referenced.VisitChildren(
                    (child, _, _) =>
                    {
                        if (child.Kind == CXCursorKind.CXCursor_AnnotateAttr)
                        {
                            var annotation = child.Spelling.ToString();
                            if (annotation.StartsWith("bank_"))
                                targetBank = annotation.Substring(5);
                        }
                        return CXChildVisitResult.CXChildVisit_Continue;
                    },
                    new CXClientData(IntPtr.Zero)
                );
            }

            if (targetBank != null)
            {
                context.Emit("PUSH r13");
                context.Emit("PUSH r14");
                context.Emit($"LDI r14, {targetBank}");

                if (isDirectCall)
                {
                    context.Emit($"LDI r13, _func_{funcName}");
                }
                else
                {
                    context.Emit("MOV r6, r15");
                    AccumulateOffset(6, indirectTargetSpillOffset, context);
                    context.Emit("LDP r13, r6");
                }

                context.Emit("CALL SYS_FAR_CALL");
                context.Emit("POP r14");
                context.Emit("POP r13");
            }
            else if (isDirectCall)
            {
                context.Emit($"CALL _func_{funcName}");
            }
            else
            {
                context.Emit("MOV r6, r15");
                AccumulateOffset(6, indirectTargetSpillOffset, context);
                context.Emit("LDP r0, r6");
                context.Emit("ALT CALL r0");
            }

            if (totalStackBytesToFree > 0)
            {
                context.Emit($"LDI r1, {totalStackBytesToFree}");
                context.Emit("CALL SYS_FREE_STACKFRAME");
            }
        }

        // restore temps
        if (tempsToProtect.Count > 0)
        {
            context.Emit("PUSH r0");
            foreach (var reg in tempsToProtect)
            {
                int offset = context.GetSpillOffset(reg);
                context.Emit("MOV r0, r15");
                AccumulateOffset(0, offset, context);
                context.Emit($"LDP r{reg}, r0");
            }
            context.Emit("POP r0");
        }

        foreach (var lease in activeLeases)
            lease.Dispose();
    }

    private static void EmitLValueAddress(CXCursor lvalue, int targetReg, EmissionContext context)
    {
        var peeled = PeelExpression(lvalue);

        // stack variable
        if (peeled.Kind == CXCursorKind.CXCursor_DeclRefExpr)
        {
            var name = peeled.Spelling.ToString();

            if (context.Locals.TryGetValue(name, out var loc))
            {
                context.Emit($"MOV r{targetReg}, r15");
                AccumulateOffset(targetReg, loc.Value, context);
            }
            else if (context.Globals.Contains(name))
            {
                context.Emit($"LDI r{targetReg}, _global_{name}");
            }
            else
            {
                throw new InvalidOperationException($"Unknown variable '{name}'");
            }
        }
        // pointer
        else if (
            peeled.Kind == CXCursorKind.CXCursor_UnaryOperator
            && GetUnaryOperatorKind(peeled) == CXUnaryOperatorKind.CXUnaryOperator_Deref
        )
        {
            var ptrExpr = GetChildren(peeled).First();
            EmitExpression(ptrExpr, targetReg, context);
        }
        // struct member
        else if (peeled.Kind == CXCursorKind.CXCursor_MemberRefExpr)
        {
            var baseExpr = GetChildren(peeled).First();
            bool isPointer = baseExpr.Type.CanonicalType.kind == CXTypeKind.CXType_Pointer;

            // Get the base address (either by dereferencing the pointer, or finding the stack struct)
            if (isPointer)
                EmitExpression(baseExpr, targetReg, context);
            else
                EmitLValueAddress(baseExpr, targetReg, context);

            var fieldDecl = clang.getCursorReferenced(peeled);

            long offsetBits = clang.Cursor_getOffsetOfField(fieldDecl);
            if (offsetBits < 0)
            {
                throw new InvalidOperationException(
                    $"Could not determine offset for struct field '{peeled.Spelling}'"
                );
            }

            // Clang gives us the offset in bits, so we divide by 8 to get bytes
            long offsetBytes = offsetBits / 8;
            AccumulateOffset(targetReg, (int)offsetBytes, context);
        }
        // struct-returning function
        else if (peeled.Kind == CXCursorKind.CXCursor_CallExpr)
        {
            throw new InvalidOperationException(
                "Call expressions are not lvalues. Aggregate call results must be consumed as expressions."
            );
        }
        // array access
        else if (peeled.Kind == CXCursorKind.CXCursor_ArraySubscriptExpr)
        {
            var children = GetChildren(peeled);
            var baseExpr = PeelExpression(children[0]);
            var indexExpr = PeelExpression(children[1]);

            if (baseExpr.Type.CanonicalType.kind == CXTypeKind.CXType_Pointer)
                EmitExpression(baseExpr, targetReg, context);
            else
                EmitLValueAddress(baseExpr, targetReg, context);

            using var indexReg = context.AcquireTempRegister();
            EmitExpression(indexExpr, indexReg.Value, context);

            var stride = peeled.Type.SizeOf;
            if (stride > 1)
            {
                using var strideReg = context.AcquireTempRegister();
                context.Emit($"LDI r{strideReg.Value}, {stride}");
                context.Emit($"MUL r{indexReg.Value}, r{strideReg.Value}");
            }

            context.Emit($"ADD r{targetReg}, r{indexReg.Value}");
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot compute memory address for expression kind: {peeled.Kind}"
            );
        }
    }

    private static void AccumulateOffset(int targetReg, int offset, EmissionContext context)
    {
        while (offset > 0)
        {
            var chunk = Math.Min(offset, 255);
            context.Emit($"IADD r{targetReg}, {chunk}");
            offset -= chunk;
        }
    }

    private static bool IsAggregateType(CXType type)
    {
        var canonicalKind = type.CanonicalType.kind;
        return canonicalKind == CXTypeKind.CXType_Record
            || canonicalKind == CXTypeKind.CXType_ConstantArray
            || canonicalKind == CXTypeKind.CXType_IncompleteArray
            || canonicalKind == CXTypeKind.CXType_VariableArray;
    }

    private static void EmitAllocStackframe(int byteCount, EmissionContext context)
    {
        if (byteCount < 0 || byteCount > 255)
            throw new InvalidOperationException($"SYS_ALLOC_STACKFRAME expects a byte count 0-255, got {byteCount}");

        context.Emit($"LDI r1, {byteCount}");
        context.Emit("CALL SYS_ALLOC_STACKFRAME"); // returns ptr in r0
    }

    private static void EmitFreeStackframe(int byteCount, EmissionContext context)
    {
        if (byteCount < 0 || byteCount > 255)
            throw new InvalidOperationException($"SYS_FREE_STACKFRAME expects a byte count 0-255, got {byteCount}");

        context.Emit($"LDI r1, {byteCount}");
        context.Emit("CALL SYS_FREE_STACKFRAME");
    }

    private static string GetOrAddStringLiteral(CXCursor stringLiteralNode, EmissionContext context)
    {
        string rawString = "";
        unsafe
        {
            var range = clang.getCursorExtent(stringLiteralNode);
            var tu = clang.Cursor_getTranslationUnit(stringLiteralNode);
            uint numTokens = 0;
            CXToken* tokens = null;

            clang.tokenize(tu, range, &tokens, &numTokens);
            if (numTokens > 0)
            {
                var cxString = clang.getTokenSpelling(tu, tokens[0]);
                rawString = cxString.ToString();
                clang.disposeString(cxString);
            }
            clang.disposeTokens(tu, tokens, numTokens);
        }

        if (!context.StringPool.TryGetValue(rawString, out var existingLabel))
        {
            existingLabel = EmissionContext.GenerateLabel("str");
            context.StringPool[rawString] = existingLabel;

            context.ReadOnlyData.Add($"{existingLabel}:");
            context.ReadOnlyData.Add($"    .DB {rawString}, 0");
        }

        return existingLabel;
    }

    private static void EmitLongToAddress(
        CXCursor expr,
        int destAddrReg,
        EmissionContext context
    )
    {
        var peeled = PeelExpression(expr);

        // Evaluate As IntegerLiteral
        var eval = peeled.Evaluate;
        if (eval.Kind == CXEvalResultKind.CXEval_Int)
        {
            long value = eval.AsLongLong;
            for (int s = 0; s < 2; s++)
            {
                using var val = context.AcquireTempRegister();
                context.Emit($"LDI r{val.Value}, {unchecked((ushort)(value & 0xFFFF))}");
                context.Emit($"STA r{val.Value}, r{destAddrReg}");
                if (s < 1)
                {
                    context.Emit($"IADD r{destAddrReg}, 2");
                    value >>= 16;
                }
            }
            return;
        }

        switch (peeled.Kind)
        {
            case CXCursorKind.CXCursor_DeclRefExpr:
                {
                    using var srcReg = context.AcquireTempRegister();
                    EmitExpression(expr, srcReg.Value, context);
                    EmitInlineAggregateCopy(srcReg.Value, destAddrReg, 4, context);
                    return;
                }

            case CXCursorKind.CXCursor_MemberRefExpr:
            case CXCursorKind.CXCursor_ArraySubscriptExpr:
                {
                    using var srcReg = context.AcquireTempRegister();
                    EmitExpression(expr, srcReg.Value, context);
                    EmitInlineAggregateCopy(srcReg.Value, destAddrReg, 4, context);
                    return;
                }

            case CXCursorKind.CXCursor_CallExpr:
                EmitCallExpressionInto(peeled, destAddrReg, context);
                return;


            case CXCursorKind.CXCursor_BinaryOperator:
                {
                    var kind = GetBinaryOperatorKind(peeled);
                    var operands = GetChildren(peeled);

                    if (
                        kind
                        is CXBinaryOperatorKind.CXBinaryOperator_Add
                            or CXBinaryOperatorKind.CXBinaryOperator_Sub
                    )
                    {
                        var lhs = PeelExpression(operands[0]);
                        var rhs = PeelExpression(operands[1]);

                        // Compute LHS value into temp1 buffer
                        using var lhsLow = context.AcquireTempRegister();
                        using var lhsHigh = context.AcquireTempRegister();

                        if (lhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
                        {
                            long v = lhs.Evaluate.AsLongLong;
                            context.Emit($"LDI r{lhsLow.Value}, {unchecked((ushort)(v & 0xFFFF))}");
                            context.Emit($"LDI r{lhsHigh.Value}, {unchecked((ushort)((v >> 16) & 0xFFFF))}");
                        }
                        else
                        {
                            using var src = context.AcquireTempRegister();
                            EmitExpression(operands[0], src.Value, context);
                            context.Emit($"LDP r{lhsLow.Value}, r{src.Value}");
                            context.Emit($"IADD r{src.Value}, 2");
                            context.Emit($"LDP r{lhsHigh.Value}, r{src.Value}");
                        }

                        // Compute RHS value into temp2 buffer
                        using var rhsLow = context.AcquireTempRegister();
                        using var rhsHigh = context.AcquireTempRegister();

                        if (rhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
                        {
                            long v = rhs.Evaluate.AsLongLong;
                            context.Emit($"LDI r{rhsLow.Value}, {unchecked((ushort)(v & 0xFFFF))}");
                            context.Emit($"LDI r{rhsHigh.Value}, {unchecked((ushort)((v >> 16) & 0xFFFF))}");
                        }
                        else
                        {
                            using var src = context.AcquireTempRegister();
                            EmitExpression(operands[1], src.Value, context);
                            context.Emit($"LDP r{rhsLow.Value}, r{src.Value}");
                            context.Emit($"IADD r{src.Value}, 2");
                            context.Emit($"LDP r{rhsHigh.Value}, r{src.Value}");
                        }

                        // Do the math
                        string op = kind == CXBinaryOperatorKind.CXBinaryOperator_Add ? "ADD" : "SUB";
                        string altOp = kind == CXBinaryOperatorKind.CXBinaryOperator_Add ? "ALT ADD" : "ALT SUB";

                        context.Emit($"{op} r{lhsLow.Value}, r{rhsLow.Value}");
                        context.Emit($"{altOp} r{lhsHigh.Value}, r{rhsHigh.Value}");

                        // Store result to destAddr
                        context.Emit($"STA r{lhsLow.Value}, r{destAddrReg}");
                        context.Emit($"IADD r{destAddrReg}, 2");
                        context.Emit($"STA r{lhsHigh.Value}, r{destAddrReg}");
                    }
                    else if (
                        kind
                        is CXBinaryOperatorKind.CXBinaryOperator_Mul
                            or CXBinaryOperatorKind.CXBinaryOperator_And
                            or CXBinaryOperatorKind.CXBinaryOperator_Or
                            or CXBinaryOperatorKind.CXBinaryOperator_Xor
                    )
                    {
                        var lhs = PeelExpression(operands[0]);
                        var rhs = PeelExpression(operands[1]);

                        using var aLow = context.AcquireTempRegister();
                        using var aHigh = context.AcquireTempRegister();
                        using var bLow = context.AcquireTempRegister();
                        using var bHigh = context.AcquireTempRegister();

                        // Load LHS (a_low, a_high)
                        if (lhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
                        {
                            long v = lhs.Evaluate.AsLongLong;
                            context.Emit($"LDI r{aLow.Value}, {unchecked((ushort)(v & 0xFFFF))}");
                            context.Emit($"LDI r{aHigh.Value}, {unchecked((ushort)((v >> 16) & 0xFFFF))}");
                        }
                        else
                        {
                            using var src = context.AcquireTempRegister();
                            EmitExpression(operands[0], src.Value, context);
                            context.Emit($"LDP r{aLow.Value}, r{src.Value}");
                            context.Emit($"IADD r{src.Value}, 2");
                            context.Emit($"LDP r{aHigh.Value}, r{src.Value}");
                        }

                        // Load RHS (b_low, b_high)
                        if (rhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
                        {
                            long v = rhs.Evaluate.AsLongLong;
                            context.Emit($"LDI r{bLow.Value}, {unchecked((ushort)(v & 0xFFFF))}");
                            context.Emit($"LDI r{bHigh.Value}, {unchecked((ushort)((v >> 16) & 0xFFFF))}");
                        }
                        else
                        {
                            using var src = context.AcquireTempRegister();
                            EmitExpression(operands[1], src.Value, context);
                            context.Emit($"LDP r{bLow.Value}, r{src.Value}");
                            context.Emit($"IADD r{src.Value}, 2");
                            context.Emit($"LDP r{bHigh.Value}, r{src.Value}");
                        }

                        if (kind == CXBinaryOperatorKind.CXBinaryOperator_Mul)
                        {
                            // 32-bit multiplication using 16-bit MUL (low) and ALT MUL (high):
                            // result_low   = low(a_low * b_low)
                            // result_high  = high(a_low * b_low) + low(a_low * b_high) + low(a_high * b_low)
                            using var origALow = context.AcquireTempRegister();
                            using var acc = context.AcquireTempRegister();

                            // Save original a_low before MUL overwrites it
                            context.Emit($"MOV r{origALow.Value}, r{aLow.Value}");

                            // Compute a_low * b_low
                            context.Emit($"MUL r{aLow.Value}, r{bLow.Value}");       // aLow = low(a_low * b_low)
                            context.Emit($"MOV r{acc.Value}, r{origALow.Value}");    // acc = original a_low
                            context.Emit($"ALT MUL r{acc.Value}, r{bLow.Value}");    // acc = high(a_low * b_low)
                                                                                     // Now: aLow = low(a_low * b_low), acc = high(a_low * b_low)

                            // acc += low(a_low * b_high) — use origALow as temp
                            context.Emit($"MUL r{origALow.Value}, r{bHigh.Value}");   // origALow = low(a_low * b_high)
                            context.Emit($"ADD r{acc.Value}, r{origALow.Value}");

                            // acc += low(a_high * b_low) — use origALow as temp
                            context.Emit($"MOV r{origALow.Value}, r{aHigh.Value}");   // origALow = a_high
                            context.Emit($"MUL r{origALow.Value}, r{bLow.Value}");    // origALow = low(a_high * b_low)
                            context.Emit($"ADD r{acc.Value}, r{origALow.Value}");

                            // Store result: aLow = result_low, acc = result_high
                            context.Emit($"STA r{aLow.Value}, r{destAddrReg}");
                            context.Emit($"IADD r{destAddrReg}, 2");
                            context.Emit($"STA r{acc.Value}, r{destAddrReg}");
                        }
                        else
                        {
                            // AND / OR / XOR — apply to both words independently
                            string op = kind switch
                            {
                                CXBinaryOperatorKind.CXBinaryOperator_And => "AND",
                                CXBinaryOperatorKind.CXBinaryOperator_Or => "OR",
                                CXBinaryOperatorKind.CXBinaryOperator_Xor => "XOR",
                                _ => throw new InvalidOperationException($"Unsupported long binary operator: {kind}")
                            };

                            context.Emit($"{op} r{aLow.Value}, r{bLow.Value}");
                            context.Emit($"{op} r{aHigh.Value}, r{bHigh.Value}");
                            context.Emit($"STA r{aLow.Value}, r{destAddrReg}");
                            context.Emit($"IADD r{destAddrReg}, 2");
                            context.Emit($"STA r{aHigh.Value}, r{destAddrReg}");
                        }
                    }
                    else if (
                        kind
                        is CXBinaryOperatorKind.CXBinaryOperator_Shl
                            or CXBinaryOperatorKind.CXBinaryOperator_Shr
                    )
                    {
                        var lhs = PeelExpression(operands[0]);
                        var rhs = PeelExpression(operands[1]);

                        // Load LHS (the long value) into aLow/aHigh
                        using var aLow = context.AcquireTempRegister();
                        using var aHigh = context.AcquireTempRegister();

                        if (lhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
                        {
                            long v = lhs.Evaluate.AsLongLong;
                            context.Emit($"LDI r{aLow.Value}, {unchecked((ushort)(v & 0xFFFF))}");
                            context.Emit($"LDI r{aHigh.Value}, {unchecked((ushort)((v >> 16) & 0xFFFF))}");
                        }
                        else
                        {
                            using var src = context.AcquireTempRegister();
                            EmitExpression(operands[0], src.Value, context);
                            context.Emit($"LDP r{aLow.Value}, r{src.Value}");
                            context.Emit($"IADD r{src.Value}, 2");
                            context.Emit($"LDP r{aHigh.Value}, r{src.Value}");
                        }

                        // Load shift amount (single register, shift count is always 16-bit)
                        using var shift = context.AcquireTempRegister();
                        var peeledRhs = PeelExpression(rhs);
                        if (peeledRhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
                        {
                            long sv = peeledRhs.Evaluate.AsLongLong;
                            if (sv == 0)
                            {
                                context.Emit($"STA r{aLow.Value}, r{destAddrReg}");
                                context.Emit($"IADD r{destAddrReg}, 2");
                                context.Emit($"STA r{aHigh.Value}, r{destAddrReg}");
                                return;
                            }
                            context.Emit($"LDI r{shift.Value}, {unchecked((ushort)sv)}");
                        }
                        else
                        {
                            EmitExpression(operands[1], shift.Value, context);
                        }

                        if (kind == CXBinaryOperatorKind.CXBinaryOperator_Shl)
                        {
                            // 32-bit SHL: result_low = low<<n, result_high = (high<<n) | (low>>(16-n))
                            using var origLow = context.AcquireTempRegister();
                            context.Emit($"MOV r{origLow.Value}, r{aLow.Value}");
                            context.Emit($"SHL r{aLow.Value}, r{shift.Value}");
                            context.Emit($"SHL r{aHigh.Value}, r{shift.Value}");
                            context.Emit($"ALT SHL r{origLow.Value}, r{shift.Value}");
                            context.Emit($"OR r{aHigh.Value}, r{origLow.Value}");
                        }
                        else
                        {
                            // 32-bit SHR: result_low = (low>>n) | (high<<(16-n)), result_high = high>>n
                            using var origHigh = context.AcquireTempRegister();
                            context.Emit($"MOV r{origHigh.Value}, r{aHigh.Value}");
                            context.Emit($"SHR r{aHigh.Value}, r{shift.Value}");
                            context.Emit($"SHR r{aLow.Value}, r{shift.Value}");
                            context.Emit($"ALT SHR r{origHigh.Value}, r{shift.Value}");
                            context.Emit($"OR r{aLow.Value}, r{origHigh.Value}");
                        }

                        context.Emit($"STA r{aLow.Value}, r{destAddrReg}");
                        context.Emit($"IADD r{destAddrReg}, 2");
                        context.Emit($"STA r{aHigh.Value}, r{destAddrReg}");
                    }
                    else
                    {
                        using var srcReg = context.AcquireTempRegister();
                        EmitExpression(expr, srcReg.Value, context);
                        EmitInlineAggregateCopy(srcReg.Value, destAddrReg, 4, context);
                    }
                    return;
                }

            case CXCursorKind.CXCursor_UnaryOperator:
                {
                    var unaryKind = GetUnaryOperatorKind(peeled);

                    // Handle long inc/dec in expressions
                    if (
                        unaryKind
                        is CXUnaryOperatorKind.CXUnaryOperator_PreInc
                            or CXUnaryOperatorKind.CXUnaryOperator_PreDec
                            or CXUnaryOperatorKind.CXUnaryOperator_PostInc
                            or CXUnaryOperatorKind.CXUnaryOperator_PostDec
                    )
                    {
                        bool isInc =
                            unaryKind is CXUnaryOperatorKind.CXUnaryOperator_PreInc
                                or CXUnaryOperatorKind.CXUnaryOperator_PostInc;
                        bool isPost =
                            unaryKind is CXUnaryOperatorKind.CXUnaryOperator_PostInc
                                or CXUnaryOperatorKind.CXUnaryOperator_PostDec;
                        var operand = GetChildren(peeled).First();
                        var op = isInc ? "INC" : "DEC";

                        // For post-inc: copy pre-value to destAddr first, then inc the variable
                        // For pre-inc: inc the variable, then copy post-value to destAddr
                        if (isPost)
                        {
                            EmitLongToAddress(operand, destAddrReg, context);
                        }

                        // Increment/decrement the original variable
                        HandleIncDec(-1, context, operand, false, op);

                        if (!isPost)
                        {
                            EmitLongToAddress(operand, destAddrReg, context);
                        }
                        return;
                    }

                    if (
                        unaryKind
                        is CXUnaryOperatorKind.CXUnaryOperator_Minus
                            or CXUnaryOperatorKind.CXUnaryOperator_Not
                    )
                    {
                        var operand = GetChildren(peeled).First();
                        bool isNeg = unaryKind == CXUnaryOperatorKind.CXUnaryOperator_Minus;

                        // Load operand value into aLow/aHigh
                        using var aLow = context.AcquireTempRegister();
                        using var aHigh = context.AcquireTempRegister();
                        var peeledOp = PeelExpression(operand);

                        if (peeledOp.Kind == CXCursorKind.CXCursor_IntegerLiteral)
                        {
                            long v = peeledOp.Evaluate.AsLongLong;
                            context.Emit($"LDI r{aLow.Value}, {unchecked((ushort)(v & 0xFFFF))}");
                            context.Emit($"LDI r{aHigh.Value}, {unchecked((ushort)((v >> 16) & 0xFFFF))}");
                        }
                        else
                        {
                            using var src = context.AcquireTempRegister();
                            EmitExpression(operand, src.Value, context);
                            context.Emit($"LDP r{aLow.Value}, r{src.Value}");
                            context.Emit($"IADD r{src.Value}, 2");
                            context.Emit($"LDP r{aHigh.Value}, r{src.Value}");
                        }

                        if (isNeg)
                        {
                            // 32-bit negation: -x = ~x + 1
                            context.Emit($"NOT r{aLow.Value}");
                            context.Emit($"NOT r{aHigh.Value}");
                            context.Emit($"ADD r{aLow.Value}, 1");
                            context.Emit($"ALT ADD r{aHigh.Value}, 0");
                        }
                        else
                        {
                            // Bitwise NOT: ~x
                            context.Emit($"NOT r{aLow.Value}");
                            context.Emit($"NOT r{aHigh.Value}");
                        }

                        context.Emit($"STA r{aLow.Value}, r{destAddrReg}");
                        context.Emit($"IADD r{destAddrReg}, 2");
                        context.Emit($"STA r{aHigh.Value}, r{destAddrReg}");
                        return;
                    }

                    // Fallback for other unary ops on long
                    using var fallbackReg = context.AcquireTempRegister();
                    EmitExpression(expr, fallbackReg.Value, context);
                    EmitInlineAggregateCopy(fallbackReg.Value, destAddrReg, 4, context);
                    return;
                }

            default:
                {
                    // Fallback: EmitExpression and copy
                    using var fallbackReg = context.AcquireTempRegister();
                    EmitExpression(expr, fallbackReg.Value, context);
                    EmitInlineAggregateCopy(fallbackReg.Value, destAddrReg, 4, context);
                    return;
                }
        }
    }

    private static void EmitCompoundLiteralIntoAddress(
        CXCursor compoundLiteralExpr,
        int destinationAddressRegister,
        EmissionContext context
    )
    {
        var initList = GetChildren(compoundLiteralExpr)
            .FirstOrDefault(c => c.Kind == CXCursorKind.CXCursor_InitListExpr);
        if (initList.Kind != CXCursorKind.CXCursor_InitListExpr)
            return;

        var initVals = GetChildren(initList);
        var decl = clang.getTypeDeclaration(compoundLiteralExpr.Type.CanonicalType);
        var fields = GetChildren(decl).Where(c => c.Kind == CXCursorKind.CXCursor_FieldDecl).ToList();

        for (int i = 0; i < initVals.Count && i < fields.Count; i++)
        {
            long offsetBytes = clang.Cursor_getOffsetOfField(fields[i]) / 8;
            long fieldSize = fields[i].Type.SizeOf;

            using var valReg = context.AcquireTempRegister();
            EmitExpression(initVals[i], valReg.Value, context);

            using var addrReg = context.AcquireTempRegister();
            context.Emit($"MOV r{addrReg.Value}, r{destinationAddressRegister}");
            AccumulateOffset(addrReg.Value, (int)offsetBytes, context);

            string altPrefix = (fieldSize == 1) ? "ALT " : "";
            context.Emit($"{altPrefix}STA r{valReg.Value}, r{addrReg.Value}");
        }
    }

    private static void EmitCompoundLiteral(CXCursor expr, int targetReg, EmissionContext context)
    {
        long size = expr.Type.SizeOf;
        var space = context.AllocateStorage(EmissionContext.GenerateLabel("anon_lit"), true, (int)size);

        using var baseReg = context.AcquireTempRegister();
        context.Emit($"MOV r{baseReg.Value}, r15");
        AccumulateOffset(baseReg.Value, space.Value, context);
        EmitCompoundLiteralIntoAddress(expr, baseReg.Value, context);

        if (targetReg >= 0)
        {
            context.Emit($"MOV r{targetReg}, r15");
            AccumulateOffset(targetReg, space.Value, context);
        }
    }
}
