using ClangSharp.Interop;
using Sharpie.CCompiler.Emitter;

namespace Sharpie.CCompiler;

public partial class SharpieEmitter
{
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
                            context.Emit($"LDI r{targetReg}, _global_{name}");
                            return;
                        }

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
                    EmitExpression(children[0], targetReg, context);
                }
                else
                {
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
            case CXCursorKind.CXCursor_DeclRefExpr:
                var name = peeled.Spelling.ToString();

                if (context.Locals.TryGetValue(name, out var loc))
                {
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

                        context.Emit($"{prefix}LDM r{mathReg}, _global_{name}");

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

                        context.Emit($"{prefix}STM r{mathReg}, _global_{name}");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Unknown variable {name}");
                }
                break;

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
            context.Emit($"LDI r{outReg}, 0");
            context.Emit($"JMP {endLabel}");
            context.Emit($"{trueLabel}:");
            context.Emit($"LDI r{outReg}, 1");
            context.Emit($"{endLabel}:");

            if (targetReg >= 0 && targetReg != outReg)
                context.Emit($"MOV r{targetReg}, r{outReg}");

            if (needsFallback)
                fallbackLease.Dispose();

            return;
        }

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

        if (kind == CXBinaryOperatorKind.CXBinaryOperator_LAnd)
        {
            EmitExpression(lhs, outReg, context);
            context.Emit($"ICMP r{outReg}, 0");
            context.Emit($"JEQ {falseLabel}");

            EmitExpression(rhs, outReg, context);
            context.Emit($"ICMP r{outReg}, 0");
            context.Emit($"JEQ {falseLabel}");

            context.Emit($"LDI r{outReg}, 1");
            context.Emit($"JMP {endLabel}");

            context.Emit($"{falseLabel}:");
            context.Emit($"LDI r{outReg}, 0");
            context.Emit($"{endLabel}:");
        }
        else
        {
            EmitExpression(lhs, outReg, context);
            context.Emit($"ICMP r{outReg}, 0");
            context.Emit($"JNE {trueLabel}");

            EmitExpression(rhs, outReg, context);
            context.Emit($"ICMP r{outReg}, 0");
            context.Emit($"JNE {trueLabel}");

            context.Emit($"LDI r{outReg}, 0");
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
}
