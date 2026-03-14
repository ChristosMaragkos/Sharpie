using ClangSharp.Interop;

namespace Sharpie.CCompiler;

public partial class SharpieEmitter
{
    private static void EmitDoStatement(CXCursor doStatement, EmissionContext context)
    {
        var children = SharpieEmitter.GetChildren(doStatement);
        var body = children[0];
        var condition = children[1];

        var labelStart = EmissionContext.GenerateLabel("do-start");
        context.Emit($"{labelStart}:");
        SharpieEmitter.EmitStatement(body, context);
        SharpieEmitter.EmitCondition(condition, labelStart, true, context);
    }

    private static void EmitForStatement(CXCursor forStatement, EmissionContext context)
    {
        var children = SharpieEmitter.GetChildren(forStatement);

        var labelStart = EmissionContext.GenerateLabel("for-start");
        var labelEnd = EmissionContext.GenerateLabel("for-end");

        var init = children[0];
        if (init.Kind != CXCursorKind.CXCursor_NoDeclFound)
            SharpieEmitter.EmitStatement(init, context);

        context.Emit($"{labelStart}:");

        var condition = children[1];
        if (condition.Kind != CXCursorKind.CXCursor_NoDeclFound)
            SharpieEmitter.EmitCondition(condition, labelEnd, false, context);
        SharpieEmitter.EmitStatement(children[3], context); // emit the body and THEN the increment

        var inc = children[2];
        if (inc.Kind != CXCursorKind.CXCursor_NoDeclFound)
        {
            using var dummy = context.AcquireTempRegister();
            SharpieEmitter.EmitExpression(inc, dummy.Value, context);
        }

        context.Emit($"JMP {labelStart}");
        context.Emit($"{labelEnd}:");
    }

    private static void EmitWhileStatement(CXCursor whileStatement, EmissionContext context)
    {
        var children = SharpieEmitter.GetChildren(whileStatement);
        var condition = children[0];
        var body = children[1];

        var labelStart = EmissionContext.GenerateLabel("while-start");
        var labelEnd = EmissionContext.GenerateLabel("while-end");

        context.Emit($"{labelStart}:");
        SharpieEmitter.EmitCondition(condition, labelEnd, false, context);
        SharpieEmitter.EmitStatement(body, context);

        context.Emit($"JMP {labelStart}");
        context.Emit($"{labelEnd}:");
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

    private static int CountLocals(CXCursor functionCursor)
    {
        var count = 0;
        var queue = new Queue<CXCursor>();
        queue.Enqueue(functionCursor);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // we count both the parameters as well as local variables
            if (current.Kind is CXCursorKind.CXCursor_VarDecl or CXCursorKind.CXCursor_ParmDecl)
                count++;

            foreach (var child in GetChildren(current))
                queue.Enqueue(child);
        }
        return count;
    }

    private static HashSet<string> DetectEscapingVariables(CXCursor functionCursor)
    {
        var escaped = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<CXCursor>();
        queue.Enqueue(functionCursor);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (
                current.Kind == CXCursorKind.CXCursor_UnaryOperator
                && GetUnaryOperatorKind(current) == CXUnaryOperatorKind.CXUnaryOperator_AddrOf
            )
            {
                var operand = PeelExpression(GetChildren(current).FirstOrDefault());

                if (operand.Kind == CXCursorKind.CXCursor_DeclRefExpr)
                    escaped.Add(operand.Spelling.ToString());
            }

            foreach (var child in GetChildren(current))
                queue.Enqueue(child);
        }
        return escaped;
    }
}
