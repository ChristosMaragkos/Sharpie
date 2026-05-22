using ClangSharp.Interop;

namespace Sharpie.CCompiler;

public partial class SharpieEmitter
{
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
                EmitMultiSlotArgAddress(arg, slots, addrReg.Value, context);

                if (addrReg.Value == 2)
                    context.Emit("MOV r1, r2");
                else if (addrReg.Value != 1)
                    context.Emit($"MOV r1, r{addrReg.Value}");

                context.Emit($"LDI r2, {slots * 2}");
                context.Emit("CALL SYS_STACKALLOC");
            }
        }

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
                EmitMultiSlotArgAddress(arg, slots, addrReg.Value, context);

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
                EmitMultiSlotArgAddress(arg, slots, addrReg.Value, context);

                if (addrReg.Value == 2)
                    context.Emit("MOV r1, r2");
                else if (addrReg.Value != 1)
                    context.Emit($"MOV r1, r{addrReg.Value}");

                context.Emit($"LDI r2, {slots * 2}");
                context.Emit("CALL SYS_STACKALLOC");
            }
        }

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
                EmitMultiSlotArgAddress(arg, slots, addrReg.Value, context);

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

        if (destAddrReg != 1)
            context.Emit($"MOV r1, r{destAddrReg}");

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

    private static void EmitMultiSlotArgAddress(CXCursor arg, int slots, int targetReg, EmissionContext context)
    {
        var peeled = PeelExpression(arg);
        bool isLvalue = peeled.Kind
            is CXCursorKind.CXCursor_DeclRefExpr
                or CXCursorKind.CXCursor_MemberRefExpr
                or CXCursorKind.CXCursor_ArraySubscriptExpr;
        isLvalue |= peeled.Kind == CXCursorKind.CXCursor_UnaryOperator
            && GetUnaryOperatorKind(peeled) == CXUnaryOperatorKind.CXUnaryOperator_Deref;

        if (isLvalue)
        {
            EmitLValueAddress(arg, targetReg, context);
        }
        else
        {
            var tempLabel = EmissionContext.GenerateLabel("arg_temp");
            var tempSpace = context.AllocateStorage(tempLabel, true, slots * 2);
            context.Emit($"MOV r{targetReg}, r15");
            AccumulateOffset(targetReg, tempSpace.Value, context);
            EmitLongToAddress(arg, targetReg, context);
            context.Emit($"MOV r{targetReg}, r15");
            AccumulateOffset(targetReg, tempSpace.Value, context);
        }
    }
}
