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

                        int lhsSlot = EmitLongToBufferIfComplex(operands[0], lhs, context);
                        int rhsSlot = EmitLongToBufferIfComplex(operands[1], rhs, context);

                        using var lhsLow = context.AcquireTempRegister();
                        using var lhsHigh = context.AcquireTempRegister();

                        if (lhsSlot >= 0)
                        {
                            LoadLongFromBuffer(lhsLow.Value, lhsHigh.Value, lhsSlot, context);
                        }
                        else if (lhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
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

                        using var rhsLow = context.AcquireTempRegister();
                        using var rhsHigh = context.AcquireTempRegister();

                        if (rhsSlot >= 0)
                        {
                            LoadLongFromBuffer(rhsLow.Value, rhsHigh.Value, rhsSlot, context);
                        }
                        else if (rhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
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

                        string op = kind == CXBinaryOperatorKind.CXBinaryOperator_Add ? "ADD" : "SUB";
                        string altOp = kind == CXBinaryOperatorKind.CXBinaryOperator_Add ? "ALT ADD" : "ALT SUB";

                        context.Emit($"{op} r{lhsLow.Value}, r{rhsLow.Value}");
                        context.Emit($"{altOp} r{lhsHigh.Value}, r{rhsHigh.Value}");

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

                        int lhsSlot = EmitLongToBufferIfComplex(operands[0], lhs, context);
                        int rhsSlot = EmitLongToBufferIfComplex(operands[1], rhs, context);

                        using var aLow = context.AcquireTempRegister();
                        using var aHigh = context.AcquireTempRegister();
                        using var bLow = context.AcquireTempRegister();
                        using var bHigh = context.AcquireTempRegister();

                        if (lhsSlot >= 0)
                        {
                            LoadLongFromBuffer(aLow.Value, aHigh.Value, lhsSlot, context);
                        }
                        else if (lhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
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

                        if (rhsSlot >= 0)
                        {
                            LoadLongFromBuffer(bLow.Value, bHigh.Value, rhsSlot, context);
                        }
                        else if (rhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
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
                            using var origALow = context.AcquireTempRegister();

                            context.Emit($"MOV r{origALow.Value}, r{aLow.Value}");

                            context.Emit($"MUL r{aLow.Value}, r{bLow.Value}");
                            context.Emit($"MOV r0, r{origALow.Value}");
                            context.Emit($"ALT MUL r0, r{bLow.Value}");

                            context.Emit($"MUL r{origALow.Value}, r{bHigh.Value}");
                            context.Emit($"ADD r0, r{origALow.Value}");

                            context.Emit($"MOV r{origALow.Value}, r{aHigh.Value}");
                            context.Emit($"MUL r{origALow.Value}, r{bLow.Value}");
                            context.Emit($"ADD r0, r{origALow.Value}");

                            context.Emit($"MOV r{aHigh.Value}, r0");
                            context.Emit($"STA r{aLow.Value}, r{destAddrReg}");
                            context.Emit($"IADD r{destAddrReg}, 2");
                            context.Emit($"STA r{aHigh.Value}, r{destAddrReg}");
                        }
                        else
                        {
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

                        int lhsSlot = EmitLongToBufferIfComplex(operands[0], lhs, context);

                        using var aLow = context.AcquireTempRegister();
                        using var aHigh = context.AcquireTempRegister();

                        if (lhsSlot >= 0)
                        {
                            LoadLongFromBuffer(aLow.Value, aHigh.Value, lhsSlot, context);
                        }
                        else if (lhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
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
                            using var origLow = context.AcquireTempRegister();
                            context.Emit($"MOV r{origLow.Value}, r{aLow.Value}");
                            context.Emit($"SHL r{aLow.Value}, r{shift.Value}");
                            context.Emit($"SHL r{aHigh.Value}, r{shift.Value}");
                            context.Emit($"ALT SHL r{origLow.Value}, r{shift.Value}");
                            context.Emit($"OR r{aHigh.Value}, r{origLow.Value}");
                        }
                        else
                        {
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
                    else if (
                        kind
                        is CXBinaryOperatorKind.CXBinaryOperator_Div
                            or CXBinaryOperatorKind.CXBinaryOperator_Rem
                    )
                    {
                        var lhs = PeelExpression(operands[0]);
                        var rhs = PeelExpression(operands[1]);

                        var tempLabel = EmissionContext.GenerateLabel("div_temp");
                        var tempSpace = context.AllocateStorage(tempLabel, true, 4);

                        using var bufAddr = context.AcquireTempRegister();
                        context.Emit($"MOV r{bufAddr.Value}, r15");
                        AccumulateOffset(bufAddr.Value, tempSpace.Value, context);

                        int lhsSlot = EmitLongToBufferIfComplex(operands[0], lhs, context);

                        if (lhsSlot >= 0)
                        {
                            LoadLongFromBuffer(2, 1, lhsSlot, context);
                        }
                        else if (lhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
                        {
                            long v = lhs.Evaluate.AsLongLong;
                            context.Emit($"LDI r1, {unchecked((ushort)((v >> 16) & 0xFFFF))}");
                            context.Emit($"LDI r2, {unchecked((ushort)(v & 0xFFFF))}");
                        }
                        else
                        {
                            using var src = context.AcquireTempRegister();
                            EmitExpression(operands[0], src.Value, context);
                            context.Emit($"LDP r2, r{src.Value}");
                            context.Emit($"IADD r{src.Value}, 2");
                            context.Emit($"LDP r1, r{src.Value}");
                        }

                        int rhsSlot = EmitLongToBufferIfComplex(operands[1], rhs, context);

                        if (rhsSlot >= 0)
                        {
                            LoadLongFromBuffer(4, 3, rhsSlot, context);
                        }
                        else if (rhs.Kind == CXCursorKind.CXCursor_IntegerLiteral)
                        {
                            long v = rhs.Evaluate.AsLongLong;
                            context.Emit($"LDI r3, {unchecked((ushort)((v >> 16) & 0xFFFF))}");
                            context.Emit($"LDI r4, {unchecked((ushort)(v & 0xFFFF))}");
                        }
                        else
                        {
                            using var src = context.AcquireTempRegister();
                            EmitExpression(operands[1], src.Value, context);
                            context.Emit($"LDP r4, r{src.Value}");
                            context.Emit($"IADD r{src.Value}, 2");
                            context.Emit($"LDP r3, r{src.Value}");
                        }

                        context.Emit($"PUSH r{destAddrReg}");

                        int mode = kind == CXBinaryOperatorKind.CXBinaryOperator_Div ? 0 : 1;
                        context.Emit($"LDI r0, {mode}");
                        context.Emit("PUSH r0");
                        context.Emit($"PUSH r{bufAddr.Value}");

                        context.Emit("CALL SYS_DIV_32");

                        context.Emit("POP r0");
                        context.Emit("POP r0");

                        context.Emit($"POP r{destAddrReg}");

                        context.Emit($"MOV r{bufAddr.Value}, r15");
                        AccumulateOffset(bufAddr.Value, tempSpace.Value, context);

                        using var resultLow = context.AcquireTempRegister();
                        using var resultHigh = context.AcquireTempRegister();
                        context.Emit($"LDP r{resultLow.Value}, r{bufAddr.Value}");
                        context.Emit($"IADD r{bufAddr.Value}, 2");
                        context.Emit($"LDP r{resultHigh.Value}, r{bufAddr.Value}");

                        context.Emit($"STA r{resultLow.Value}, r{destAddrReg}");
                        context.Emit($"IADD r{destAddrReg}, 2");
                        context.Emit($"STA r{resultHigh.Value}, r{destAddrReg}");
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
                        bool isNeg = unaryKind == CXUnaryOperatorKind.CXUnaryOperator_Minus;

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
                            context.Emit($"NOT r{aLow.Value}");
                            context.Emit($"NOT r{aHigh.Value}");
                            context.Emit($"ADD r{aLow.Value}, 1");
                            context.Emit($"ALT ADD r{aHigh.Value}, 0");
                        }
                        else
                        {
                            context.Emit($"NOT r{aLow.Value}");
                            context.Emit($"NOT r{aHigh.Value}");
                        }

                        context.Emit($"STA r{aLow.Value}, r{destAddrReg}");
                        context.Emit($"IADD r{destAddrReg}, 2");
                        context.Emit($"STA r{aHigh.Value}, r{destAddrReg}");
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

    private static bool IsComplexLongExpression(CXCursor peeled)
    {
        return peeled.Kind == CXCursorKind.CXCursor_BinaryOperator
            || peeled.Kind == CXCursorKind.CXCursor_UnaryOperator;
    }
}
