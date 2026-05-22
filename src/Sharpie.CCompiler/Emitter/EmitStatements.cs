using ClangSharp.Interop;

namespace Sharpie.CCompiler;

public partial class SharpieEmitter
{
    private static void EmitReturn(CXCursor returnStmt, EmissionContext context)
    {
        var expr = GetChildren(returnStmt).FirstOrDefault();

        if (expr.Kind != CXCursorKind.CXCursor_NoDeclFound)
        {
            long retSizeBytes = expr.Type.SizeOf;

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
            else
            {
                EmitExpression(expr, 0, context);
            }
        }

        context.Emit($"JMP {context.EpilogueLabel}");
        context.HasReturn = true;
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

                    context.Emit($"LDP r{aLow.Value}, r{lhsBase.Value}");
                    context.Emit($"IADD r{lhsBase.Value}, 2");
                    context.Emit($"LDP r{aHigh.Value}, r{lhsBase.Value}");

                    context.Emit($"LDP r{bLow.Value}, r{rhsBase.Value}");
                    context.Emit($"IADD r{rhsBase.Value}, 2");
                    context.Emit($"LDP r{bHigh.Value}, r{rhsBase.Value}");

                    var labelCmpDone = EmissionContext.GenerateLabel("cmp_done");

                    context.Emit($"CMP r{aHigh.Value}, r{bHigh.Value}");
                    context.Emit($"JNE {labelCmpDone}");

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
}
