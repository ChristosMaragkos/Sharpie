using System.Text;
using ClangSharp.Interop;

namespace Sharpie.CCompiler;

public sealed class SharpieEmitter
{
    private const int TempRegisterStart = 1;
    private const int TempRegisterEnd = 7;
    private const int LocalRegisterStart = 8;
    private const int LocalRegisterEnd = 15;

    public string EmitTranslationUnit(CXCursor translationUnitCursor)
    {
        var asm = new StringBuilder();
        asm.AppendLine(".REGION FIXED");

        var functions = GetChildren(translationUnitCursor)
            .Where(c => c.Kind == CXCursorKind.CXCursor_FunctionDecl)
            .ToList();

        var mainFunctions = functions.Where(func => func.Spelling.ToString() == "main");
        if (mainFunctions.Count() > 1)
            throw new InvalidOperationException(
                "Ambiguous entrypoint: more than one 'main' function found."
            );

        foreach (var func in functions)
        {
            var funcName = func.Spelling.ToString();
            var body = GetChildren(func)
                .FirstOrDefault(c => c.Kind == CXCursorKind.CXCursor_CompoundStmt);

            // Skip functions without a body (prototypes)
            if (body.Kind == CXCursorKind.CXCursor_NoDeclFound)
                continue;

            // Create a FRESH context for every function which is okay because the label counter is static
            var context = new EmissionContext();
            context.IsMain = (funcName == "main");

            var parameters = GetChildren(func)
                .Where(c => c.Kind == CXCursorKind.CXCursor_ParmDecl)
                .ToList();

            for (int i = 0; i < parameters.Count; i++)
            {
                var paramName = parameters[i].Spelling.ToString();
                // Map param 1 to r1, param 2 to r2
                // We add them to the locals so the compiler knows where they live
                // FIXME: This is a really bad way to handle this. Fix once calling conventions, stack semantics and actual function handling is implemented
                context.Locals[paramName] = i + 1;
            }

            asm.AppendLine($"{(context.IsMain ? "Main" : funcName)}:");

            EmitFunctionBody(body, context);

            // If the function reached the end without a return,
            // emit an implicit one (HALT for main, RET for anything else)
            if (!context.HasReturn)
            {
                if (context.IsMain)
                    context.Emit("    LDI r0, 0");
                context.Emit(context.ReturnInstruction);
            }

            foreach (var line in context.Instructions)
                asm.AppendLine($"    {line}");
        }

        asm.AppendLine(".ENDREGION");
        return asm.ToString();
    }

    private static void EmitFunctionBody(CXCursor compoundStmt, EmissionContext context)
    {
        foreach (var stmt in GetChildren(compoundStmt))
        {
            if (context.HasReturn)
                throw new InvalidOperationException("Dead code is not supported in this MVP");

            EmitStatement(stmt, context);
        }
    }

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

    private static void EmitDoStatement(CXCursor doStatement, EmissionContext context)
    {
        var children = GetChildren(doStatement);
        var body = children[0];
        var condition = children[1];

        var labelStart = EmissionContext.GenerateLabel();
        context.Emit($"{labelStart}:");
        EmitStatement(body, context);
        EmitCondition(condition, labelStart, true, context);
    }

    private static void EmitForStatement(CXCursor forStatement, EmissionContext context)
    {
        var children = GetChildren(forStatement);

        var labelStart = EmissionContext.GenerateLabel();
        var labelEnd = EmissionContext.GenerateLabel();

        var init = children[0];
        if (init.Kind != CXCursorKind.CXCursor_NoDeclFound)
            EmitStatement(init, context);

        context.Emit($"{labelStart}:");

        var condition = children[1];
        if (condition.Kind != CXCursorKind.CXCursor_NoDeclFound)
            EmitCondition(condition, labelEnd, false, context);

        EmitStatement(children[3], context); // emit the body and THEN the increment

        var inc = children[2];
        if (inc.Kind != CXCursorKind.CXCursor_NoDeclFound)
        {
            using var dummy = context.AcquireTempRegister();
            EmitExpression(inc, dummy.Value, context);
        }

        context.Emit($"JMP {labelStart}");
        context.Emit($"{labelEnd}:");
    }

    private static void EmitWhileStatement(CXCursor whileStatement, EmissionContext context)
    {
        var children = GetChildren(whileStatement);
        var condition = children[0];
        var body = children[1];

        var labelStart = EmissionContext.GenerateLabel();
        var labelEnd = EmissionContext.GenerateLabel();

        context.Emit($"{labelStart}:");
        EmitCondition(condition, labelEnd, false, context);
        EmitStatement(body, context);

        context.Emit($"JMP {labelStart}");
        context.Emit($"{labelEnd}:");
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

        var targetReg = context.AllocateLocalRegister();
        context.Locals[variableName] = targetReg;

        var initExpr = GetChildren(varDecl).FirstOrDefault();
        if (initExpr.Kind == CXCursorKind.CXCursor_NoDeclFound)
        {
            context.Emit($"LDI r{targetReg}, 0");
            return;
        }

        EmitExpression(initExpr, targetReg, context);
    }

    private static void EmitAssignmentStatement(CXCursor assignmentCursor, EmissionContext context)
    {
        var children = GetChildren(assignmentCursor);
        if (children.Count != 2)
            throw new InvalidOperationException("Expected assignment to have exactly 2 operands.");

        var lhs = PeelExpression(children[0]);
        var rhs = PeelExpression(children[1]);

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
        if (!context.Locals.TryGetValue(variableName, out var lhsReg))
            throw new InvalidOperationException($"Unknown local variable `{variableName}`.");

        if (assignmentCursor.Kind == CXCursorKind.CXCursor_CompoundAssignOperator)
        {
            EmitCompoundAssignment(assignmentCursor, lhsReg, rhs, context);
            return;
        }

        if (GetBinaryOperatorKind(assignmentCursor) != CXBinaryOperatorKind.CXBinaryOperator_Assign)
            throw new InvalidOperationException(
                "Only simple assignment statements are currently supported."
            );

        EmitExpression(rhs, lhsReg, context);
    }

    private static void EmitCompoundAssignment(
        CXCursor assignmentCursor,
        int lhsReg,
        CXCursor rhs,
        EmissionContext context
    )
    {
        var kind = GetBinaryOperatorKind(assignmentCursor);

        if (TryEmitImmediateMath(kind, lhsReg, rhs, context))
            return;

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
            _ => throw new InvalidOperationException($"Unsupported compound assignment: {kind}"),
        };

        context.Emit($"{op} r{lhsReg}, r{scratch.Value}");
    }

    private static void EmitReturn(CXCursor returnStmt, EmissionContext context)
    {
        var expr = GetChildren(returnStmt).FirstOrDefault();
        if (expr.Kind != CXCursorKind.CXCursor_NoDeclFound)
            EmitExpression(expr, 0, context);

        context.Emit(context.ReturnInstruction);
        context.HasReturn = true;
    }

    private static void EmitExpression(CXCursor expr, int targetReg, EmissionContext context)
    {
        var node = PeelExpression(expr);

        switch (node.Kind)
        {
            case CXCursorKind.CXCursor_IntegerLiteral:
                context.Emit($"LDI r{targetReg}, {unchecked((ushort)GetLiteralValue(node))}");
                return;

            case CXCursorKind.CXCursor_CallExpr:
                EmitCallExpression(node, targetReg, context);
                return;

            case CXCursorKind.CXCursor_DeclRefExpr:
                var name = node.Spelling.ToString();
                if (!context.Locals.TryGetValue(name, out var sourceReg))
                    throw new InvalidOperationException($"Unknown local variable `{name}`.");
                if (sourceReg != targetReg)
                    context.Emit($"MOV r{targetReg}, r{sourceReg}");
                return;

            case CXCursorKind.CXCursor_UnaryOperator:
                EmitUnaryExpression(node, targetReg, context);
                return;

            case CXCursorKind.CXCursor_BinaryOperator:
                EmitBinaryExpression(node, targetReg, context);
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
            // 1. Handle Local Variable (Register-backed)
            case CXCursorKind.CXCursor_DeclRefExpr:
            {
                var name = peeled.Spelling.ToString();
                if (!context.Locals.TryGetValue(name, out var varReg))
                    throw new InvalidOperationException($"Unknown variable {name}");

                if (isPost)
                {
                    if (targetReg >= 0)
                        context.Emit($"MOV r{targetReg}, r{varReg}");
                    context.Emit($"{op} r{varReg}");
                }
                else
                {
                    context.Emit($"{op} r{varReg}");
                    if (targetReg >= 0)
                        context.Emit($"MOV r{targetReg}, r{varReg}");
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

                // Load value from memory
                context.Emit($"LDP r{targetReg}, r{addrReg.Value}");

                using var newValReg = context.AcquireTempRegister();
                context.Emit($"MOV r{newValReg.Value}, r{targetReg}");
                context.Emit($"{op} r{newValReg.Value}");

                // Write back to memory
                context.Emit($"STA r{newValReg.Value}, r{addrReg.Value}");

                // If it was PRE, we need to return the NEW value in targetReg
                if (!isPost)
                {
                    context.Emit($"MOV r{targetReg}, r{newValReg.Value}");
                }

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

        EmitExpression(lhs, targetReg, context);

        if (TryEmitImmediateMath(kind, targetReg, rhs, context))
            return;

        using var scratch = context.AcquireTempRegister();
        EmitExpression(rhs, scratch.Value, context);

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

        context.Emit($"{op} r{targetReg}, r{scratch.Value}");
    }

    private static void EmitIfStatement(CXCursor ifStatement, EmissionContext context)
    {
        var children = GetChildren(ifStatement);

        var condition = children[0];
        var thenBranch = children[1];
        var hasElse = children.Count > 2;

        var labelEnd = EmissionContext.GenerateLabel();
        var labelElse = hasElse ? EmissionContext.GenerateLabel() : labelEnd;

        EmitCondition(condition, labelElse, false, context);

        EmitFunctionBody(thenBranch, context);

        if (hasElse)
        {
            if (!context.HasReturn)
                context.Emit($"JMP {labelEnd}");

            context.Emit($"{labelElse}:");
            context.HasReturn = false;
            EmitFunctionBody(children[2], context);
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

    // TODO: This does not take calling conventions into account, so it still needs caller- and callee-saved registers etc.
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

        // Evaluate arguments and place them in the parameter registers
        // r1 and r2 are our convention for passing data to functions for now (see TODO)
        for (int i = 1; i < children.Count; i++)
        {
            if (i > 2)
                throw new InvalidOperationException("Sharpie MVP only supports 2 arguments.");

            EmitExpression(children[i], i, context);
        }

        context.Emit($"CALL {funcName}");

        if (targetReg > 0)
        {
            context.Emit($"MOV r{targetReg}, r0"); // MOV r0, r0 is redundant
        }
    }

    private static bool TryEmitImmediateMath(
        CXBinaryOperatorKind kind,
        int targetReg,
        CXCursor rhs,
        EmissionContext context
    )
    {
        if (!TryGetByteLiteral(rhs, out var value))
            return false;

        var immediateOp = kind switch
        {
            CXBinaryOperatorKind.CXBinaryOperator_Add
            or CXBinaryOperatorKind.CXBinaryOperator_AddAssign => "IADD",
            CXBinaryOperatorKind.CXBinaryOperator_Sub
            or CXBinaryOperatorKind.CXBinaryOperator_SubAssign => "ISUB",
            CXBinaryOperatorKind.CXBinaryOperator_Mul
            or CXBinaryOperatorKind.CXBinaryOperator_MulAssign => "IMUL",
            CXBinaryOperatorKind.CXBinaryOperator_Div
            or CXBinaryOperatorKind.CXBinaryOperator_DivAssign => "IDIV",
            CXBinaryOperatorKind.CXBinaryOperator_Rem
            or CXBinaryOperatorKind.CXBinaryOperator_RemAssign => "IMOD",
            CXBinaryOperatorKind.CXBinaryOperator_And
            or CXBinaryOperatorKind.CXBinaryOperator_AndAssign => "IAND",
            CXBinaryOperatorKind.CXBinaryOperator_Or
            or CXBinaryOperatorKind.CXBinaryOperator_OrAssign => "IOR",
            CXBinaryOperatorKind.CXBinaryOperator_Xor
            or CXBinaryOperatorKind.CXBinaryOperator_XorAssign => "IXOR",
            _ => null,
        };

        if (immediateOp is null)
            return false;

        context.Emit($"{immediateOp} r{targetReg}, {value}");
        return true;
    }

    private static bool TryGetByteLiteral(CXCursor cursor, out byte value)
    {
        value = 0;

        var peeled = PeelExpression(cursor);
        if (peeled.Kind != CXCursorKind.CXCursor_IntegerLiteral)
            return false;

        var literal = GetLiteralValue(peeled);
        if (literal < 0 || literal > byte.MaxValue)
            return false;

        value = (byte)literal;
        return true;
    }

    private static long GetLiteralValue(CXCursor integerLiteralCursor)
    {
        return integerLiteralCursor.Evaluate.AsLongLong;
    }

    private static CXCursor PeelExpression(CXCursor cursor)
    {
        var current = cursor;

        while (
            current.Kind
                is CXCursorKind.CXCursor_UnexposedExpr
                    or CXCursorKind.CXCursor_ParenExpr
                    or CXCursorKind.CXCursor_CStyleCastExpr
        )
        {
            var next = GetChildren(current).FirstOrDefault();
            if (next.Kind == CXCursorKind.CXCursor_NoDeclFound)
                break;
            current = next;
        }

        return current;
    }

    private static CXBinaryOperatorKind GetBinaryOperatorKind(CXCursor cursor)
    {
        return clang.getCursorBinaryOperatorKind(cursor);
    }

    private static CXUnaryOperatorKind GetUnaryOperatorKind(CXCursor cursor)
    {
        return clang.getCursorUnaryOperatorKind(cursor);
    }

    private static void ValidateMainSignature(CXCursor mainCursor)
    {
        if (mainCursor.ResultType.kind != CXTypeKind.CXType_Int)
            throw new InvalidOperationException("Only `int main(void)` is currently supported.");

        var hasParameters = GetChildren(mainCursor)
            .Any(c => c.Kind == CXCursorKind.CXCursor_ParmDecl);

        if (hasParameters)
            throw new InvalidOperationException(
                "Only zero-parameter `main` is currently supported."
            );
    }

    private static List<CXCursor> GetChildren(CXCursor cursor)
    {
        var children = new List<CXCursor>();

        unsafe
        {
            cursor.VisitChildren(
                (child, _, _) =>
                {
                    children.Add(child);
                    return CXChildVisitResult.CXChildVisit_Continue;
                },
                new CXClientData(IntPtr.Zero)
            );
        }

        return children;
    }

    private sealed class EmissionContext
    {
        private readonly bool[] _tempInUse = new bool[TempRegisterEnd - TempRegisterStart + 1];
        private int _nextLocalRegister = LocalRegisterStart;

        private static int _labelCount;

        public static string GenerateLabel() => $"_L{_labelCount++}";

        public Dictionary<string, int> Locals { get; } = new(StringComparer.Ordinal);
        public List<string> Instructions { get; } = [];
        public bool HasReturn { get; set; }

        public bool IsMain { get; set; }
        public string ReturnInstruction => IsMain ? "HALT" : "RET";

        public void Emit(string asm)
        {
            Instructions.Add(asm);
        }

        public int AllocateLocalRegister()
        {
            if (_nextLocalRegister > LocalRegisterEnd)
                throw new InvalidOperationException(
                    "Too many local variables for register-only MVP backend."
                );

            return _nextLocalRegister++;
        }

        public TempLease AcquireTempRegister()
        {
            for (var i = 0; i < _tempInUse.Length; i++)
            {
                if (_tempInUse[i])
                    continue;

                _tempInUse[i] = true;
                return new TempLease(this, TempRegisterStart + i);
            }

            throw new InvalidOperationException(
                "Expression is too complex for temporary register budget."
            );
        }

        private void ReleaseTempRegister(int register)
        {
            var index = register - TempRegisterStart;
            if (index < 0 || index >= _tempInUse.Length)
                throw new InvalidOperationException(
                    $"Attempted to release invalid temp register r{register}."
                );

            _tempInUse[index] = false;
        }

        public readonly struct TempLease : IDisposable
        {
            private readonly EmissionContext _ctx;
            public int Value { get; }

            public TempLease(EmissionContext ctx, int value)
            {
                _ctx = ctx;
                Value = value;
            }

            public void Dispose()
            {
                _ctx.ReleaseTempRegister(Value);
            }
        }
    }
}
