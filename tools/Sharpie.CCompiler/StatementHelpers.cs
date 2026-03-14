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
        var typeKind = varDecl.Type.kind;
        var isSupported =
            typeKind
            is CXTypeKind.CXType_Int
                or CXTypeKind.CXType_UInt
                or CXTypeKind.CXType_Pointer;

        if (!isSupported)
            throw new InvalidOperationException(
                $"Type `{varDecl.Type.Spelling}` is not supported for local variables in this MVP."
            );

        var variableName = varDecl.Spelling.ToString();
        if (string.IsNullOrWhiteSpace(variableName))
            throw new InvalidOperationException("Encountered unnamed local variable.");

        if (context.Locals.ContainsKey(variableName))
            throw new InvalidOperationException($"Duplicate local variable `{variableName}`.");

        var needsStack = context.EscapedVariables.Contains(variableName);
        var space = context.AllocateStorage(variableName, needsStack);

        var initExpr = GetChildren(varDecl).FirstOrDefault();
        using var valReg = context.AcquireTempRegister();

        if (initExpr.Kind is CXCursorKind.CXCursor_NoDeclFound)
            context.Emit($"LDI r{valReg.Value}, 0");
        else
            EmitExpression(initExpr, valReg.Value, context);

        if (space.Type == StorageType.Register)
        {
            context.Emit($"MOV r{space.Value}, r{valReg.Value}");
        }
        else
        {
            using var offsetReg = context.AcquireTempRegister();
            context.Emit($"LDI r{offsetReg.Value}, {space.Value}");
            context.Emit($"STS r{valReg.Value}, r{offsetReg.Value}");
        }
    }

    private static void EmitAssignmentStatement(CXCursor assignmentCursor, EmissionContext context)
    {
        var children = GetChildren(assignmentCursor);
        if (children.Count != 2)
            throw new InvalidOperationException("Expected assignment to have exactly 2 operands.");

        var lhs = PeelExpression(children[0]);
        var rhs = PeelExpression(children[1]);

        // Handle *ptr = value
        if (
            lhs.Kind == CXCursorKind.CXCursor_UnaryOperator
            && GetUnaryOperatorKind(lhs) == CXUnaryOperatorKind.CXUnaryOperator_Deref
        )
        {
            using var valReg = context.AcquireTempRegister();
            using var addrReg = context.AcquireTempRegister();

            EmitExpression(rhs, valReg.Value, context);

            var ptrOperand = GetChildren(lhs).FirstOrDefault();
            EmitExpression(ptrOperand, addrReg.Value, context);

            context.Emit($"STA r{valReg.Value}, r{addrReg.Value}");
            return;
        }

        if (lhs.Kind != CXCursorKind.CXCursor_DeclRefExpr)
            throw new InvalidOperationException(
                "Only assignments to local variables are supported."
            );

        var variableName = lhs.Spelling.ToString();
        if (!context.Locals.TryGetValue(variableName, out var allocatedSpace))
            throw new InvalidOperationException($"Unknown local variable `{variableName}`.");

        if (assignmentCursor.Kind == CXCursorKind.CXCursor_CompoundAssignOperator)
        {
            EmitCompoundAssignment(assignmentCursor, allocatedSpace, rhs, context);
            return;
        }

        if (allocatedSpace.Type == StorageType.Stack)
        {
            using var rhsReg = context.AcquireTempRegister();
            EmitExpression(rhs, rhsReg.Value, context);

            using var offsetReg = context.AcquireTempRegister();
            context.Emit($"LDI r{offsetReg.Value}, {allocatedSpace}");
            context.Emit($"STS r{rhsReg.Value}, r{offsetReg.Value}");
        }
        else
        {
            EmitExpression(rhs, allocatedSpace.Value, context);
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
            EmitExpression(expr, 0, context);

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
            // case CXCursorKind.CXCursor_IntegerLiteral:
            //     context.Emit($"LDI r{targetReg}, {unchecked((ushort)GetLiteralValue(node))}");
            //     return;

            case CXCursorKind.CXCursor_CallExpr:
                EmitCallExpression(node, targetReg, context);
                return;

            case CXCursorKind.CXCursor_DeclRefExpr:
                var name = node.Spelling.ToString();
                if (!context.Locals.TryGetValue(name, out var allocatedSpace))
                    throw new InvalidOperationException($"Unknown local variable `{name}`.");

                // Only load from the stack if someone is actually asking for the value
                if (targetReg >= 0)
                {
                    if (allocatedSpace.Type == StorageType.Stack)
                    {
                        using var offsetReg = context.AcquireTempRegister();
                        context.Emit($"LDI r{offsetReg.Value}, {allocatedSpace}");
                        context.Emit($"LDS r{targetReg}, r{offsetReg.Value}");
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

            // case CXCursorKind
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
                var varName = peeled.Spelling.ToString();
                if (!context.Locals.TryGetValue(varName, out var allocated))
                    throw new InvalidOperationException(
                        $"Unknown variable `{varName}` for address-of."
                    );

                if (allocated.Type == StorageType.Register)
                    throw new InvalidOperationException(
                        $"Cannot take the address of register-allocated '{varName}'"
                    );

                context.Emit($"GETSP r{targetReg}");

                var remainingOffset = allocated.Value;
                while (remainingOffset > 0)
                {
                    var chunk = Math.Min(remainingOffset, 255);
                    context.Emit($"IADD r{targetReg}, {chunk}");
                    remainingOffset -= chunk;
                }
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

        using var lhsScratch = context.AcquireTempRegister();
        EmitExpression(lhs, lhsScratch.Value, context);

        if (!TryEmitImmediateMath(kind, lhsScratch.Value, rhs, context))
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

            context.Emit($"{op} r{lhsScratch.Value}, r{rhsScratch.Value}");
        }

        if (targetReg >= 0 && targetReg != lhsScratch.Value)
            context.Emit($"MOV r{targetReg}, r{lhsScratch.Value}");
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
        var funcName = children[0].Spelling.ToString();

        // identify which temporary registers are in use to protect them
        var tempsToProtect = context.GetActiveTempRegisters();
        foreach (var reg in tempsToProtect)
            context.Emit($"PUSH r{reg}");

        // if we have 5+ args push to the stack
        for (int i = children.Count - 1; i >= 5; i--)
        {
            using (var argLease = context.AcquireTempRegister())
            {
                EmitExpression(children[i], argLease.Value, context);
                context.Emit($"PUSH r{argLease.Value}");
            } // Leases are disposed at this boundary, r1 is free for the next argument
        }

        // handle args 1-4 by shoving them into registers. TODO: Refactor this once I implement structs.
        var regArgLeases = new List<EmissionContext.TempLease>();
        int numRegArgs = Math.Min(children.Count - 1, 4);

        for (int i = 1; i <= numRegArgs; i++)
        {
            var lease = context.AcquireTempRegister();
            EmitExpression(children[i], lease.Value, context);
            regArgLeases.Add(lease);
        }

        for (int i = 0; i < regArgLeases.Count; i++)
        {
            int targetRegIdx = i + 1;
            if (targetRegIdx != regArgLeases[i].Value)
                context.Emit($"MOV r{targetRegIdx}, r{regArgLeases[i].Value}");
        }

        if (!TryEmitIntrinsic(funcName, targetReg, context))
        {
            context.Emit($"CALL {funcName}");

            // Cleanup the stack if we pushed excess args. TODO: Merge this with SYS_FREE_STACKFRAME of callee
            int excessArgs = (children.Count - 1) - 4;
            if (excessArgs > 0)
            {
                context.Emit($"LDI r1, {excessArgs * 2}");
                context.Emit("CALL SYS_FREE_STACKFRAME");
            }
        }

        for (int i = tempsToProtect.Count - 1; i >= 0; i--)
            context.Emit($"POP r{tempsToProtect[i]}");

        if (targetReg > 0)
            context.Emit($"MOV r{targetReg}, r0");

        foreach (var lease in regArgLeases)
            lease.Dispose();
    }
}
