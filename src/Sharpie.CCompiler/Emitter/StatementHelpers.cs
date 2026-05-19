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
                    long stride = clang.getElementType(varDecl.Type).SizeOf;
                    if (stride <= 0)
                        stride = 2; // Fallback

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
            else if (initExprs.Count > 0)
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

            using var srcAddrReg = context.AcquireTempRegister();
            EmitExpression(rhs, srcAddrReg.Value, context); // must yield address for aggregates

            using var destAddrReg2 = context.AcquireTempRegister();
            EmitLValueAddress(lhs, destAddrReg2.Value, context);

            context.Emit($"PUSH r{srcAddrReg.Value}");
            context.Emit($"MOV r1, r{destAddrReg2.Value}");
            context.Emit("POP r2");
            context.Emit($"LDI r3, {assignSize}");
            context.Emit("CALL SYS_MEM_MOVE");
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

    private static void EmitCompoundAssignment(
        CXCursor assignmentCursor,
        CXCursor lhs,
        CXCursor rhs,
        EmissionContext context
    )
    {
        var kind = GetBinaryOperatorKind(assignmentCursor);

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

            case CXCursorKind.CXCursor_MemberRefExpr:
                if (targetReg >= 0)
                {
                    if (TryEmitMemberReadFromStructReturnCall(node, targetReg, context))
                        return;

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
            return false;

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
                // Standard *ptr read
                EmitExpression(operand, targetReg, context);
                context.Emit($"LDP r{targetReg}, r{targetReg}");
                return;

            case CXUnaryOperatorKind.CXUnaryOperator_Minus:
                EmitExpression(operand, targetReg, context);
                context.Emit($"NEG r{targetReg}");
                return;

            case CXUnaryOperatorKind.CXUnaryOperator_Not:
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
                    EmissionContext.TempLease valRegLease = default;
                    EmissionContext.TempLease addrLease = default;

                    if (loc.Type == StorageType.Register)
                    {
                        mathReg = loc.Value;
                    }
                    else
                    {
                        valRegLease = context.AcquireTempRegister();
                        addrLease = context.AcquireTempRegister();
                        mathReg = valRegLease.Value;

                        context.Emit($"MOV r{addrLease.Value}, r15");
                        AccumulateOffset(addrLease.Value, loc.Value, context);
                        context.Emit($"LDP r{mathReg}, r{addrLease.Value}");
                    }

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

                    if (loc.Type == StorageType.Stack)
                    {
                        context.Emit($"STA r{mathReg}, r{addrLease.Value}");
                        valRegLease.Dispose();
                        addrLease.Dispose();
                    }
                }
                else if (context.Globals.Contains(name))
                {
                    // --- GLOBAL VARIABLE ---
                    using var valRegLease = context.AcquireTempRegister();
                    int mathReg = valRegLease.Value;

                    var isByte = peeled.Type.SizeOf == 1;
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

                var jumpMnemonic = GetJumpMnemonic(kind, jumpIfTrue);
                context.Emit($"{jumpMnemonic} {targetLabel}");
                return;
            }
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
        }

        if (totalStackBytesToFree > 0)
        {
            context.Emit($"LDI r1, {totalStackBytesToFree}");
            context.Emit("CALL SYS_FREE_STACKFRAME");
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

    private static void EmitCompoundLiteral(CXCursor expr, int targetReg, EmissionContext context)
    {
        long size = expr.Type.SizeOf;
        var space = context.AllocateStorage(EmissionContext.GenerateLabel("anon_lit"), true, (int)size);

        var initList = GetChildren(expr).FirstOrDefault(c => c.Kind == CXCursorKind.CXCursor_InitListExpr);
        var initVals = GetChildren(initList);

        using var baseReg = context.AcquireTempRegister();
        context.Emit($"MOV r{baseReg.Value}, r15");
        AccumulateOffset(baseReg.Value, space.Value, context);

        var decl = clang.getTypeDeclaration(expr.Type.CanonicalType);
        var fields = GetChildren(decl).Where(c => c.Kind == CXCursorKind.CXCursor_FieldDecl).ToList();

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

        if (targetReg >= 0)
        {
            context.Emit($"MOV r{targetReg}, r15");
            AccumulateOffset(targetReg, space.Value, context);
        }
    }
}
