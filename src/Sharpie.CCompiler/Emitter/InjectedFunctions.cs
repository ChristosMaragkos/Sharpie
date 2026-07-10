namespace Sharpie.CCompiler.Emitter;

internal static class InjectedFunctions
{
    private const string FuncPrefix = "_func___injected_32bit_";

    private static readonly (string Name, string Assembly)[] Functions =
    [
        ("add", """
            ADD r2, r4
            ALT ADD r1, r3
            RET
            """),
        ("sub", """
            SUB r2, r4
            ALT SUB r1, r3
            RET
            """),
        ("mul", """
            MOV r5, r2
            MUL r2, r4
            MOV r0, r5
            ALT MUL r0, r4
            XOR r6, r6
            CMP r5, r6
            JGE _mul_c1
            ADD r0, r4
            _mul_c1:
            CMP r4, r6
            JGE _mul_c2
            ADD r0, r5
            _mul_c2:
            MUL r5, r3
            ADD r0, r5
            MOV r5, r1
            MUL r5, r4
            ADD r0, r5
            MOV r1, r0
            RET
            """),
        ("and", """
            AND r2, r4
            AND r1, r3
            RET
            """),
        ("or", """
            OR r2, r4
            OR r1, r3
            RET
            """),
        ("xor", """
            XOR r2, r4
            XOR r1, r3
            RET
            """),
        ("shl", """
            MOV r5, r2
            SHL r2, r4
            SHL r1, r4
            ALT SHL r5, r4
            OR r1, r5
            RET
            """),
        ("shr", """
            MOV r5, r1
            SHR r1, r4
            SHR r2, r4
            ALT SHR r5, r4
            OR r2, r5
            RET
            """),
        ("neg", """
            NOT r2
            NOT r1
            ADD r2, 1
            ALT ADD r1, 0
            RET
            """),
        ("not", """
            NOT r2
            NOT r1
            RET
            """),
        ("div", """
            CALL _func___injected_32bit_divmod
            MOV r1, r11
            MOV r2, r12
            RET
            """),
        ("mod", """
            CALL _func___injected_32bit_divmod
            MOV r1, r7
            MOV r2, r5
            RET
            """),
        ("divmod", """
            ; r1:r2 = A (high:low), r3:r4 = B (high:low)
            ; returns r11:r12 = quotient, r7:r5 = remainder
            XOR r6, r6              ; sign flags (bit0=A neg, bit1=B neg)
            ICMP r1, 0
            JGE _dm_bpos
            LDI r6, 1
            NOT r2
            NOT r1
            ADD r2, 1
            ALT ADD r1, 0
            _dm_bpos:
            ICMP r3, 0
            JGE _dm_init
            LDI r0, 2
            OR r6, r0
            NOT r4
            NOT r3
            ADD r4, 1
            ALT ADD r3, 0
            _dm_init:
            XOR r7, r7
            XOR r5, r5
            XOR r11, r11
            XOR r12, r12
            XOR r9, r9
            _dm_loop:
            ADD r5, r5
            ALT ADD r7, r7
            ADD r2, r2
            ALT ADD r1, r1
            JNC _dm_nb
            IOR r5, 1
            _dm_nb:
            ADD r12, r12
            ALT ADD r11, r11
            CMP r7, r3
            JNE _dm_hd
            CMP r5, r4
            _dm_hd:
            JC _dm_ss
            SUB r5, r4
            ALT SUB r7, r3
            IOR r12, 1
            _dm_ss:
            INC r9
            ICMP r9, 32
            JLT _dm_loop
            MOV r9, r6
            LDI r0, 1
            SHR r9, r0
            XOR r9, r6
            AND r9, r0
            ICMP r9, 1
            JNE _dm_cr
            NOT r12
            NOT r11
            ADD r12, 1
            ALT ADD r11, 0
            _dm_cr:
            LDI r0, 1
            MOV r9, r6
            AND r9, r0
            ICMP r9, 1
            JNE _dm_done
            NOT r5
            NOT r7
            ADD r5, 1
            ALT ADD r7, 0
            _dm_done:
            RET
            """),
        ("eq", """
            XOR r0, r0
            CMP r1, r3
            JNE _eq_done
            CMP r2, r4
            JNE _eq_done
            LDI r0, 1
            _eq_done:
            MOV r2, r0
            XOR r1, r1
            RET
            """),
        ("neq", """
            XOR r0, r0
            CMP r1, r3
            JNE _neq_true
            CMP r2, r4
            JEQ _neq_done
            _neq_true:
            LDI r0, 1
            _neq_done:
            MOV r2, r0
            XOR r1, r1
            RET
            """),
        ("lt", """
            XOR r0, r0
            CMP r1, r3
            JLT _lt_true
            JGT _lt_done
            LDI r5, 0x8000
            XOR r2, r5
            XOR r4, r5
            CMP r2, r4
            JLT _lt_true
            JMP _lt_done
            _lt_true:
            LDI r0, 1
            _lt_done:
            MOV r2, r0
            XOR r1, r1
            RET
            """),
        ("gt", """
            XOR r0, r0
            CMP r1, r3
            JGT _gt_true
            JLT _gt_done
            LDI r5, 0x8000
            XOR r2, r5
            XOR r4, r5
            CMP r2, r4
            JGT _gt_true
            JMP _gt_done
            _gt_true:
            LDI r0, 1
            _gt_done:
            MOV r2, r0
            XOR r1, r1
            RET
            """),
        ("le", """
            CMP r1, r3
            JLT _le_true
            JGT _le_false
            LDI r5, 0x8000
            XOR r2, r5
            XOR r4, r5
            CMP r2, r4
            JLE _le_true
            _le_false:
            XOR r0, r0
            JMP _le_done
            _le_true:
            LDI r0, 1
            _le_done:
            MOV r2, r0
            XOR r1, r1
            RET
            """),
        ("ge", """
            CMP r1, r3
            JGT _ge_true
            JLT _ge_false
            LDI r5, 0x8000
            XOR r2, r5
            XOR r4, r5
            CMP r2, r4
            JGE _ge_true
            _ge_false:
            XOR r0, r0
            JMP _ge_done
            _ge_true:
            LDI r0, 1
            _ge_done:
            MOV r2, r0
            XOR r1, r1
            RET
            """),
    ];

    public static string GenerateAll()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("; --- Injected 32-bit Operations ---");
        foreach (var (name, asm) in Functions)
        {
            sb.AppendLine($"{FuncPrefix}{name}:");
            foreach (var line in asm.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                sb.AppendLine($"    {line}");
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
