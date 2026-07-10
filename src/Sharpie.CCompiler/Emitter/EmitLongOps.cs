using ClangSharp.Interop;
using Sharpie.CCompiler.Emitter;

namespace Sharpie.CCompiler;

public partial class SharpieEmitter
{
    private static void EmitLongToAddress(
        CXCursor expr,
        int destAddrReg,
        EmissionContext context
    )
    {
        var peeled = PeelExpression(expr);

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

                        string name = kind == CXBinaryOperatorKind.CXBinaryOperator_Add ? "add" : "sub";

                        context.Emit($"PUSH r{destAddrReg}");
                        LoadLongToHighLow(operands[0], lhs, 1, 2, context);
                        LoadLongToHighLow(operands[1], rhs, 3, 4, context);
                        context.Emit($"CALL _func___injected_32bit_{name}");
                        context.Emit("POP r0");
                        context.Emit("STA r2, r0");
                        context.Emit("IADD r0, 2");
                        context.Emit("STA r1, r0");
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

                        string name = kind switch
                        {
                            CXBinaryOperatorKind.CXBinaryOperator_Mul => "mul",
                            CXBinaryOperatorKind.CXBinaryOperator_And => "and",
                            CXBinaryOperatorKind.CXBinaryOperator_Or => "or",
                            CXBinaryOperatorKind.CXBinaryOperator_Xor => "xor",
                            _ => throw new InvalidOperationException($"Unsupported long binary operator: {kind}")
                        };

                        context.Emit($"PUSH r{destAddrReg}");
                        LoadLongToHighLow(operands[0], lhs, 1, 2, context);
                        LoadLongToHighLow(operands[1], rhs, 3, 4, context);
                        context.Emit($"CALL _func___injected_32bit_{name}");
                        context.Emit("POP r0");
                        context.Emit("STA r2, r0");
                        context.Emit("IADD r0, 2");
                        context.Emit("STA r1, r0");
                    }
                    else if (
                        kind
                        is CXBinaryOperatorKind.CXBinaryOperator_Shl
                            or CXBinaryOperatorKind.CXBinaryOperator_Shr
                    )
                    {
                        var lhs = PeelExpression(operands[0]);
                        var rhs = PeelExpression(operands[1]);

                        string name = kind == CXBinaryOperatorKind.CXBinaryOperator_Shl ? "shl" : "shr";

                        context.Emit($"PUSH r{destAddrReg}");
                        using var aHighS = context.AcquireTempRegister();
                        using var aLowS = context.AcquireTempRegister();
                        LoadLongToHighLow(operands[0], lhs, aHighS.Value, aLowS.Value, context);

                        var peeledRhs = PeelExpression(rhs);
                        if (peeledRhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
                        {
                            long sv = peeledRhs.Evaluate.AsLongLong;
                            if (sv == 0)
                            {
                                context.Emit("POP r0");
                                context.Emit("STA r2, r0");
                                context.Emit("IADD r0, 2");
                                context.Emit("STA r1, r0");
                                return;
                            }
                            context.Emit($"LDI r4, {unchecked((ushort)sv)}");
                            context.Emit($"XOR r3, r3");
                        }
                        else
                        {
                            using var src = context.AcquireTempRegister();
                            EmitExpression(operands[1], src.Value, context);
                            context.Emit($"MOV r4, r{src.Value}");
                            context.Emit($"XOR r3, r3");
                        }

                        context.Emit($"CALL _func___injected_32bit_{name}");
                        context.Emit("POP r0");
                        context.Emit("STA r2, r0");
                        context.Emit("IADD r0, 2");
                        context.Emit("STA r1, r0");
                    }
                    else if (
                        kind
                        is CXBinaryOperatorKind.CXBinaryOperator_Div
                            or CXBinaryOperatorKind.CXBinaryOperator_Rem
                    )
                    {
                        var lhs = PeelExpression(operands[0]);
                        var rhs = PeelExpression(operands[1]);

                        string name = kind == CXBinaryOperatorKind.CXBinaryOperator_Div ? "div" : "mod";

                        context.Emit($"PUSH r{destAddrReg}");
                        LoadLongToHighLow(operands[0], lhs, 1, 2, context);
                        LoadLongToHighLow(operands[1], rhs, 3, 4, context);
                        context.Emit($"CALL _func___injected_32bit_{name}");
                        context.Emit("POP r0");
                        context.Emit("STA r2, r0");
                        context.Emit("IADD r0, 2");
                        context.Emit("STA r1, r0");
                    }
                    else if (
                        kind
                        is CXBinaryOperatorKind.CXBinaryOperator_EQ
                            or CXBinaryOperatorKind.CXBinaryOperator_NE
                            or CXBinaryOperatorKind.CXBinaryOperator_LT
                            or CXBinaryOperatorKind.CXBinaryOperator_GT
                            or CXBinaryOperatorKind.CXBinaryOperator_LE
                            or CXBinaryOperatorKind.CXBinaryOperator_GE
                    )
                    {
                        var lhs = PeelExpression(operands[0]);
                        var rhs = PeelExpression(operands[1]);

                        string name = kind switch
                        {
                            CXBinaryOperatorKind.CXBinaryOperator_EQ => "eq",
                            CXBinaryOperatorKind.CXBinaryOperator_NE => "neq",
                            CXBinaryOperatorKind.CXBinaryOperator_LT => "lt",
                            CXBinaryOperatorKind.CXBinaryOperator_GT => "gt",
                            CXBinaryOperatorKind.CXBinaryOperator_LE => "le",
                            CXBinaryOperatorKind.CXBinaryOperator_GE => "ge",
                            _ => throw new InvalidOperationException($"Unsupported long comparison: {kind}")
                        };

                        context.Emit($"PUSH r{destAddrReg}");
                        LoadLongToHighLow(operands[0], lhs, 1, 2, context);
                        LoadLongToHighLow(operands[1], rhs, 3, 4, context);
                        context.Emit($"CALL _func___injected_32bit_{name}");
                        context.Emit("POP r0");
                        context.Emit("STA r2, r0");
                        context.Emit("IADD r0, 2");
                        context.Emit("STA r1, r0");
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

                        if (isPost)
                        {
                            EmitLongToAddress(operand, destAddrReg, context);
                        }

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
                        string name = unaryKind == CXUnaryOperatorKind.CXUnaryOperator_Minus ? "neg" : "not";

                        context.Emit($"PUSH r{destAddrReg}");
                        using var aHighU = context.AcquireTempRegister();
                        using var aLowU = context.AcquireTempRegister();
                        LoadLongToHighLow(operand, PeelExpression(operand), aHighU.Value, aLowU.Value, context);
                        context.Emit($"CALL _func___injected_32bit_{name}");
                        context.Emit("POP r0");
                        context.Emit("STA r2, r0");
                        context.Emit("IADD r0, 2");
                        context.Emit("STA r1, r0");
                        return;
                    }

                    using var fallbackReg = context.AcquireTempRegister();
                    EmitExpression(expr, fallbackReg.Value, context);
                    EmitInlineAggregateCopy(fallbackReg.Value, destAddrReg, 4, context);
                    return;
                }

            default:
                {
                    using var fallbackReg = context.AcquireTempRegister();
                    EmitExpression(expr, fallbackReg.Value, context);
                    EmitInlineAggregateCopy(fallbackReg.Value, destAddrReg, 4, context);
                    return;
                }
        }
    }

    

    private static int EmitLongToBufferIfComplex(CXCursor operand, CXCursor peeled, EmissionContext context)
    {
        if (!IsComplexLongExpression(peeled))
            return -1;

        var label = EmissionContext.GenerateLabel("buf");
        var slot = context.AllocateStorage(label, true, 4);

        context.Emit("MOV r0, r15");
        AccumulateOffset(0, slot.Value, context);
        EmitLongToAddress(operand, 0, context);
        return slot.Value;
    }

    private static void LoadLongFromBuffer(int lowReg, int highReg, int slot, EmissionContext context)
    {
        context.Emit("MOV r0, r15");
        AccumulateOffset(0, slot, context);
        context.Emit($"LDP r{lowReg}, r0");
        context.Emit($"IADD r0, 2");
        context.Emit($"LDP r{highReg}, r0");
    }

    private static void LoadLongToHighLow(CXCursor operand, CXCursor peeled, int highReg, int lowReg, EmissionContext context)
    {
        int slot = EmitLongToBufferIfComplex(operand, peeled, context);
        if (slot >= 0)
        {
            LoadLongFromBuffer(lowReg, highReg, slot, context);
        }
        else if (peeled.Kind == CXCursorKind.CXCursor_IntegerLiteral)
        {
            long v = peeled.Evaluate.AsLongLong;
            context.Emit($"LDI r{lowReg}, {unchecked((ushort)(v & 0xFFFF))}");
            context.Emit($"LDI r{highReg}, {unchecked((ushort)((v >> 16) & 0xFFFF))}");
        }
        else
        {
            EmitExpression(operand, 0, context);
            context.Emit($"LDP r{lowReg}, r0");
            context.Emit($"IADD r0, 2");
            context.Emit($"LDP r{highReg}, r0");
        }
    }

    private static bool IsComplexLongExpression(CXCursor peeled)
    {
        return peeled.Kind == CXCursorKind.CXCursor_BinaryOperator
            || peeled.Kind == CXCursorKind.CXCursor_UnaryOperator;
    }
}
