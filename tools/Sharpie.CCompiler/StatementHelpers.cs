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
                    throw new InvalidOperationException(
                        "Break statement outside of a loop or switch."
                    );
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
                unsafe
                {
                    // clang doesn't tokenize the string literal in asm statements so we have to tokenize and iterate ourselves
                    var range = clang.getCursorExtent(stmt);
                    var tu = clang.Cursor_getTranslationUnit(stmt);

                    uint numTokens = 0;
                    CXToken* tokens = null;

                    clang.tokenize(tu, range, &tokens, &numTokens);

                    for (uint i = 0; i < numTokens; i++)
                    {
                        var token = tokens[i];

                        if (clang.getTokenKind(token) is CXTokenKind.CXToken_Literal)
                        {
                            var cxString = clang.getTokenSpelling(tu, token);
                            var asmString = cxString.ToString();

                            clang.disposeString(cxString);

                            asmString = asmString.Trim('"');
                            asmString = asmString.Replace("\\n", "\n").Replace("\\t", "\t");

                            var asmLines = asmString.Split(
                                '\n',
                                StringSplitOptions.RemoveEmptyEntries
                            );
                            foreach (var line in asmLines)
                            {
                                context.Emit(line.Trim());
                            }
                        }
                    }

                    clang.disposeTokens(tu, tokens, numTokens);
                }
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
                throw new InvalidOperationException(
                    $"Unsupported declaration kind in statement: {declaration.Kind}"
                );

            EmitVariableDeclaration(declaration, context);
        }
    }

    private static void EmitVariableDeclaration(CXCursor varDecl, EmissionContext context)
    {
        var variableName = varDecl.Spelling.ToString();
        if (string.IsNullOrWhiteSpace(variableName))
            throw new InvalidOperationException("Encountered unnamed local variable.");

        if (context.Locals.ContainsKey(variableName))
            throw new InvalidOperationException($"Duplicate local variable `{variableName}`.");

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

        // Identify actual inline initialization for arrays/structs
        var initList = GetChildren(varDecl)
            .FirstOrDefault(c => c.Kind == CXCursorKind.CXCursor_InitListExpr);

        if (isRecord || isArray)
        {
            if (initList.Kind != CXCursorKind.CXCursor_NoDeclFound)
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
            return; // done initializing
        }

        var initExprs = GetChildren(varDecl)
            .Where(c =>
                c.Kind >= CXCursorKind.CXCursor_FirstExpr
                && c.Kind <= CXCursorKind.CXCursor_LastExpr
            )
            .ToList();

        using var valRegPrimitive = context.AcquireTempRegister();

        if (initExprs.Count == 0)
            context.Emit($"LDI r{valRegPrimitive.Value}, 0");
        else
            EmitExpression(initExprs.Last(), valRegPrimitive.Value, context);

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

        // Fast Path: Direct local variable in a register (r8-r15)
        if (lhs.Kind == CXCursorKind.CXCursor_DeclRefExpr)
        {
            var variableName = lhs.Spelling.ToString();
            if (
                context.Locals.TryGetValue(variableName, out var loc)
                && loc.Type == StorageType.Register
            )
            {
                if (assignmentCursor.Kind == CXCursorKind.CXCursor_CompoundAssignOperator)
                    EmitCompoundAssignment(assignmentCursor, loc, rhs, context);
                else
                    EmitExpression(rhs, loc.Value, context);

                return;
            }
        }

        // Standard Path: Memory Assignment
        using var valReg = context.AcquireTempRegister();
        EmitExpression(rhs, valReg.Value, context);

        using var addrReg = context.AcquireTempRegister();
        EmitLValueAddress(lhs, addrReg.Value, context);

        var assignSize = lhs.Type.SizeOf;
        if (assignSize > 2)
        {
            // Delegate to BIOS memcpy
            // PUSH and POP to prevent overlapping register mapping bugs (e.g., if valReg=1 and addrReg=2)
            context.Emit($"PUSH r{valReg.Value}");
            context.Emit($"MOV r1, r{addrReg.Value}"); // Dest
            context.Emit("POP r2"); // Source
            context.Emit($"LDI r3, {assignSize}");
            context.Emit("CALL SYS_MEM_MOVE");
        }
        else
        {
            var prefix = (assignSize == 1) ? "ALT " : "";
            // Standard Word Assignment
            context.Emit($"{prefix}STA r{valReg.Value}, r{addrReg.Value}");
        }
    }

    private static void EmitCompoundAssignment(
        CXCursor assignmentCursor,
        StorageLocation lhsLoc,
        CXCursor rhs,
        EmissionContext context
    )
    {
        var kind = GetBinaryOperatorKind(assignmentCursor);

        int mathReg;
        EmissionContext.TempLease valRegLease = default;
        EmissionContext.TempLease offsetRegLease = default;

        if (lhsLoc.Type == StorageType.Register)
        {
            mathReg = lhsLoc.Value;
        }
        else
        {
            valRegLease = context.AcquireTempRegister();
            offsetRegLease = context.AcquireTempRegister();
            mathReg = valRegLease.Value;

            context.Emit($"LDI r{offsetRegLease.Value}, {lhsLoc.Value}");
            context.Emit($"LDS r{mathReg}, r{offsetRegLease.Value}");
        }

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

        // write back (only if it lives on the stack)
        if (lhsLoc.Type == StorageType.Stack)
        {
            context.Emit($"STS r{mathReg}, r{offsetRegLease.Value}");
            valRegLease.Dispose();
            offsetRegLease.Dispose();
        }
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
                using var srcReg = context.AcquireTempRegister();
                EmitLValueAddress(expr, srcReg.Value, context);

                context.Emit($"PUSH r{srcReg.Value}"); // Save the struct's address safely to the stack
                context.Emit($"MOV r1, r{context.HiddenRetPtrReg}"); // Overwrite r1 with the Hidden Pointer
                context.Emit("POP r2"); // Retrieve the struct's address securely into r2
                context.Emit($"LDI r3, {retSizeBytes}"); // Byte count
                context.Emit("CALL SYS_MEM_MOVE");
            }
            else // Normal 16-bit return
            {
                EmitExpression(expr, 0, context);
            }
        }

        foreach (var line in context.GetEpilogue())
            context.Emit(line);

        context.Emit(context.ReturnInstruction);
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
            case CXCursorKind.CXCursor_StringLiteral:
                if (targetReg >= 0)
                {
                    var strLabel = EmissionContext.GenerateLabel("str");

                    unsafe
                    {
                        var range = clang.getCursorExtent(node);
                        var tu = clang.Cursor_getTranslationUnit(node);
                        uint numTokens = 0;
                        CXToken* tokens = null;

                        clang.tokenize(tu, range, &tokens, &numTokens);
                        if (numTokens > 0)
                        {
                            var cxString = clang.getTokenSpelling(tu, tokens[0]);
                            var rawString = cxString.ToString();
                            clang.disposeString(cxString);

                            if (!context.StringPool.TryGetValue(rawString, out var existingLabel))
                            {
                                existingLabel = EmissionContext.GenerateLabel("str");
                                context.StringPool[rawString] = existingLabel;

                                context.ReadOnlyData.Add($"{strLabel}:");
                                context.ReadOnlyData.Add($"    .DB {rawString}, 0");
                            }
                            context.Emit($"LDI r{targetReg}, {existingLabel}");
                        }
                        clang.disposeTokens(tu, tokens, numTokens);
                    }
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

                if (!context.Locals.TryGetValue(name, out var allocatedSpace))
                    throw new InvalidOperationException($"Unknown local variable `{name}`.");

                // Only load from the stack if someone is actually asking for the value
                if (targetReg >= 0)
                {
                    if (node.Type.CanonicalType.kind is CXTypeKind.CXType_ConstantArray)
                    {
                        context.Emit($"MOV R{targetReg}, r15");
                        AccumulateOffset(targetReg, allocatedSpace.Value, context);
                        return;
                    }

                    var isByte = node.Type.SizeOf == 1;
                    var prefix = isByte ? "ALT " : "";

                    if (allocatedSpace.Type == StorageType.Stack)
                    {
                        using var offsetReg = context.AcquireTempRegister();
                        context.Emit($"LDI r{offsetReg.Value}, {allocatedSpace}");
                        context.Emit($"{prefix}LDS r{targetReg}, r{offsetReg.Value}");
                    }
                    else
                    {
                        if (targetReg != allocatedSpace.Value)
                            context.Emit($"MOV r{targetReg}, r{allocatedSpace.Value}");
                    }
                }
                return;

            case CXCursorKind.CXCursor_UnaryOperator:
                EmitUnaryExpression(node, targetReg, context);
                return;

            case CXCursorKind.CXCursor_BinaryOperator:
                EmitBinaryExpression(node, targetReg, context);
                return;

            case CXCursorKind.CXCursor_MemberRefExpr:
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
        }

        throw new InvalidOperationException($"Unsupported expression kind: {node.Kind}");
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
            // Handle Local Variable (Stack-backed)
            case CXCursorKind.CXCursor_DeclRefExpr:
            {
                var name = peeled.Spelling.ToString();
                if (!context.Locals.TryGetValue(name, out var loc))
                    throw new InvalidOperationException($"Unknown variable {name}");

                int mathReg;
                EmissionContext.TempLease valRegLease = default;
                EmissionContext.TempLease offsetRegLease = default;

                if (loc.Type == StorageType.Register)
                {
                    mathReg = loc.Value;
                }
                else
                {
                    valRegLease = context.AcquireTempRegister();
                    offsetRegLease = context.AcquireTempRegister();
                    mathReg = valRegLease.Value;

                    context.Emit($"LDI r{offsetRegLease.Value}, {loc.Value}");
                    context.Emit($"LDS r{mathReg}, r{offsetRegLease.Value}");
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
                    context.Emit($"STS r{mathReg}, r{offsetRegLease.Value}");
                    valRegLease.Dispose();
                    offsetRegLease.Dispose();
                }
                break;
            }
            // 2. Handle Pointer Dereference (*ptr)++
            case CXCursorKind.CXCursor_UnaryOperator
                when GetUnaryOperatorKind(peeled) == CXUnaryOperatorKind.CXUnaryOperator_Deref:
            {
                using var addrReg = context.AcquireTempRegister();
                var ptrExpr = GetChildren(peeled).First();
                EmitExpression(ptrExpr, addrReg.Value, context);

                // We must use a temp register to load the value so we don't emit "LDP r-1"
                using var valReg = context.AcquireTempRegister();

                // Load value from memory
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

                // Write back to memory
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
        if (kind == CXBinaryOperatorKind.CXBinaryOperator_Assign)
            throw new InvalidOperationException("Assignment is only supported as a statement.");

        var operands = GetChildren(binaryExpr);
        if (operands.Count != 2)
            throw new InvalidOperationException(
                "Binary expression must have exactly two operands."
            );

        var lhs = PeelExpression(operands[0]);
        var rhs = PeelExpression(operands[1]);

        // if the caller already leased a temp register (r1-r7) evaluate the LHS directly inside it.
        bool needsFallback = targetReg < TempRegisterStart || targetReg > TempRegisterEnd;
        EmissionContext.TempLease fallbackLease = default;
        var lhsReg = targetReg;

        if (needsFallback)
        {
            fallbackLease = context.AcquireTempRegister();
            lhsReg = fallbackLease.Value;
        }

        EmitExpression(lhs, lhsReg, context);

        if (!TryEmitImmediateMath(kind, lhsReg, rhs, context))
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

            context.Emit($"{op} r{lhsReg}, r{rhsScratch.Value}");
        }

        if (targetReg >= 0 && targetReg != lhsReg)
            context.Emit($"MOV r{targetReg}, r{lhsReg}");

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
            var operands = GetChildren(node);
            var lhs = PeelExpression(operands[0]);
            var rhs = PeelExpression(operands[1]);

            using var leftReg = context.AcquireTempRegister();
            EmitExpression(lhs, leftReg.Value, context);

            if (TryGetByteLiteral(rhs, out var immValue))
                context.Emit($"ICMP r{leftReg.Value}, {immValue}");
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

        using var reg = context.AcquireTempRegister();
        EmitExpression(node, reg.Value, context);
        context.Emit($"ICMP r{reg.Value}, 0");
        context.Emit(jumpIfTrue ? $"JNE {targetLabel}" : $"JEQ {targetLabel}");
    }

    private static object GetJumpMnemonic(CXBinaryOperatorKind kind, bool jumpIfTrue)
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
        StorageLocation tempRetSpace = default;

        if (hasHiddenPtr)
        {
            // Allocate a temporary local variable to hold the returned struct
            // Because we stitch the Prologue at the end, TotalStackBytes will cleanly account for this.
            tempRetSpace = context.AllocateStorage(
                EmissionContext.GenerateLabel("tmp_ret"),
                true,
                (int)retSize
            );
        }

        var activeLeases = new List<EmissionContext.TempLease>();

        if (!isDirectCall)
        {
            using var calleeRegLease = context.AcquireTempRegister();
            EmitExpression(callee, calleeRegLease.Value, context);
            context.Emit($"PUSH r{calleeRegLease.Value}");
        }

        var tempsToProtect = context.GetActiveTempRegisters();
        foreach (var reg in tempsToProtect)
            context.Emit($"PUSH r{reg}");

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

        foreach (var assignment in regAssignments)
        {
            if (assignment.TargetReg != assignment.SourceTempReg)
                context.Emit($"MOV r{assignment.TargetReg}, r{assignment.SourceTempReg}");
        }

        if (hasHiddenPtr)
        {
            context.Emit("MOV r1, r15");
            AccumulateOffset(1, tempRetSpace.Value, context);
        }

        if (!TryEmitIntrinsic(funcName, targetReg, context))
        {
            if (isDirectCall)
                context.Emit($"CALL _func_{funcName}");
            else
            {
                context.Emit("POP r0");
                context.Emit("ALT CALL r0");
            }

            if (totalStackBytesToFree > 0)
            {
                context.Emit($"LDI r1, {totalStackBytesToFree}");
                context.Emit("CALL SYS_FREE_STACKFRAME");
            }
        }

        for (int i = tempsToProtect.Count - 1; i >= 0; i--)
            context.Emit($"POP r{tempsToProtect[i]}");

        if (targetReg > 0)
        {
            // Yield the address of the struct, not r0
            if (hasHiddenPtr)
            {
                context.Emit($"MOV r{targetReg}, r15");
                AccumulateOffset(targetReg, tempRetSpace.Value, context);
            }
            else
            {
                context.Emit($"MOV r{targetReg}, r0");
            }
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
            var loc = context.Locals[name];

            if (loc.Type == StorageType.Register)
                throw new InvalidOperationException(
                    $"Cannot take address of register-allocated '{name}'"
                );

            context.Emit($"MOV r{targetReg}, r15");
            AccumulateOffset(targetReg, loc.Value, context);
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
                throw new InvalidOperationException(
                    $"Could not determine offset for struct field '{peeled.Spelling}'"
                );

            // Clang gives us the offset in bits, so we divide by 8 to get bytes
            long offsetBytes = offsetBits / 8;
            AccumulateOffset(targetReg, (int)offsetBytes, context);
        }
        // struct-returning function
        else if (peeled.Kind == CXCursorKind.CXCursor_CallExpr)
        {
            EmitCallExpression(peeled, targetReg, context);
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
}
