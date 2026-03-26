using ClangSharp.Interop;

namespace Sharpie.CCompiler;

public partial class SharpieEmitter
{
    private static void EmitDoStatement(CXCursor doStatement, EmissionContext context)
    {
        var children = GetChildren(doStatement);
        var body = children[0];
        var condition = children[1];

        var labelStart = EmissionContext.GenerateLabel("do_start");
        var labelCond = EmissionContext.GenerateLabel("do_cond");
        var labelEnd = EmissionContext.GenerateLabel("do_end");

        context.Emit($"{labelStart}:");

        context.BreakLabels.Push(labelEnd);
        context.ContinueLabels.Push(labelCond);
        EmitStatement(body, context);
        context.ContinueLabels.Pop();
        context.BreakLabels.Pop();

        context.Emit($"{labelCond}:"); // Continue jumps here
        EmitCondition(condition, labelStart, true, context);
        context.Emit($"{labelEnd}:"); // Break jumps here
    }

    private static void EmitForStatement(CXCursor forStatement, EmissionContext context)
    {
        var children = GetChildren(forStatement);

        var labelStart = EmissionContext.GenerateLabel("for_start");
        var labelInc = EmissionContext.GenerateLabel("for_inc"); // NEW: For 'continue'
        var labelEnd = EmissionContext.GenerateLabel("for_end");

        var init = children[0];
        if (init.Kind != CXCursorKind.CXCursor_NoDeclFound)
            EmitStatement(init, context);

        context.Emit($"{labelStart}:");

        var condition = children[1];
        if (condition.Kind != CXCursorKind.CXCursor_NoDeclFound)
            EmitCondition(condition, labelEnd, false, context);

        context.BreakLabels.Push(labelEnd);
        context.ContinueLabels.Push(labelInc);
        EmitStatement(children[3], context);
        context.ContinueLabels.Pop();
        context.BreakLabels.Pop();

        context.Emit($"{labelInc}:"); // Continue jumps here
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

        var labelStart = EmissionContext.GenerateLabel("while_start");
        var labelEnd = EmissionContext.GenerateLabel("while_end");

        context.Emit($"{labelStart}:");
        EmitCondition(condition, labelEnd, false, context);

        context.BreakLabels.Push(labelEnd);
        context.ContinueLabels.Push(labelStart);
        EmitStatement(body, context);
        context.ContinueLabels.Pop();
        context.BreakLabels.Pop();

        context.Emit($"JMP {labelStart}");
        context.Emit($"{labelEnd}:");
    }

    private static void EmitSwitchStatement(CXCursor switchStmt, EmissionContext context)
    {
        var children = GetChildren(switchStmt);
        var condition = children[0];
        var body = children[1]; // Usually a CompoundStmt

        var labelEnd = EmissionContext.GenerateLabel("switch_end");
        context.BreakLabels.Push(labelEnd);

        using var condReg = context.AcquireTempRegister();
        EmitExpression(condition, condReg.Value, context);

        var bodyStmts = GetChildren(body);
        var cases = new List<(long Value, string Label)>();
        string? defaultLabel = null;

        foreach (var stmt in bodyStmts)
        {
            if (stmt.Kind == CXCursorKind.CXCursor_CaseStmt)
            {
                var valExpr = PeelExpression(GetChildren(stmt).First());
                long val = GetLiteralValue(valExpr);
                var caseLabel = EmissionContext.GenerateLabel($"case_{val}");
                cases.Add((val, caseLabel));
            }
            else if (stmt.Kind == CXCursorKind.CXCursor_DefaultStmt)
            {
                defaultLabel = EmissionContext.GenerateLabel("default");
            }
        }

        long minVal = cases.Count > 0 ? cases.Min(c => c.Value) : 0;
        long maxVal = cases.Count > 0 ? cases.Max(c => c.Value) : 0;
        long range = maxVal - minVal;

        // Only use a jump table if the range is 64 or less
        if (cases.Count > 0 && range <= 64)
        {
            var jtLabel = EmissionContext.GenerateLabel("jt");
            string safeDefault = defaultLabel ?? labelEnd;

            // bounds checking (if it's out of bounds, jump to default)
            context.Emit($"ICMP r{condReg.Value}, {minVal}");
            context.Emit($"JLT {safeDefault}");
            context.Emit($"ICMP r{condReg.Value}, {maxVal}");
            context.Emit($"JGT {safeDefault}");

            // normalize index: offset = (condition - min) * 2 bytes
            if (minVal != 0)
                context.Emit($"ISUB r{condReg.Value}, {minVal}");
            context.Emit($"IMUL r{condReg.Value}, 2");

            // add base address of the jump table
            using var baseReg = context.AcquireTempRegister();
            context.Emit($"LDI r{baseReg.Value}, {jtLabel}");
            context.Emit($"ADD r{condReg.Value}, r{baseReg.Value}");

            // Dereference pointer to get the actual case label address
            context.Emit($"LDP r{condReg.Value}, r{condReg.Value}");

            context.Emit($"ALT JMP r{condReg.Value}");

            context.ReadOnlyData.Add($"{jtLabel}:");
            for (long i = minVal; i <= maxVal; i++)
            {
                var (value, label) = cases.FirstOrDefault(c => c.Value == i);
                if (label != null)
                    context.ReadOnlyData.Add($"    .DW {label}");
                else
                    context.ReadOnlyData.Add($"    .DW {safeDefault}"); // Fill missing gaps with default
            }
        }
        else
        {
            // sparse switches
            foreach (var (val, label) in cases)
            {
                context.Emit($"ICMP r{condReg.Value}, {val}");
                context.Emit($"JEQ {label}");
            }
            if (defaultLabel != null)
                context.Emit($"JMP {defaultLabel}");
            else
                context.Emit($"JMP {labelEnd}");
        }

        int caseIdx = 0;
        foreach (var stmt in bodyStmts)
        {
            if (stmt.Kind == CXCursorKind.CXCursor_CaseStmt)
            {
                context.Emit($"{cases[caseIdx].Label}:");
                EmitStatement(stmt, context);
                caseIdx++;
            }
            else if (stmt.Kind == CXCursorKind.CXCursor_DefaultStmt)
            {
                context.Emit($"{defaultLabel}:");
                EmitStatement(stmt, context);
            }
            else
            {
                EmitStatement(stmt, context);
            }
        }

        context.Emit($"{labelEnd}:");
        context.BreakLabels.Pop();
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

    private static int GetRegistersNeededForVariable(CXType type)
    {
        var sizeBytes = type.SizeOf;
        if (sizeBytes < 0)
            return 1;

        return (int)((sizeBytes + 1) / 2);
    }
}
