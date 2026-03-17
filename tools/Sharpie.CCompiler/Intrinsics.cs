namespace Sharpie.CCompiler;

public partial class SharpieEmitter
{
    private static bool TryEmitIntrinsic(string funcName, int targetReg, EmissionContext context)
    {
        var biosAliases = new Dictionary<string, string>
        {
            { "__sharpie_alloca", "SYS_ALLOC_STACKFRAME" },
            { "__sharpie_stackalloc", "SYS_STACKALLOC" },
            { "__sharpie_delay", "SYS_FRAME_DELAY" },
            { "__sharpie_memcpy", "SYS_MEM_COPY" },
            { "__sharpie_memset", "SYS_MEM_SET" },
            { "__sharpie_memcmp", "SYS_MEM_CMP" },
            { "__sharpie_pal_reset", "SYS_PAL_RESET" },
        };

        if (biosAliases.TryGetValue(funcName, out var syscall))
        {
            context.Emit($"CALL {syscall}");
            return true;
        }

        switch (funcName)
        {
            // --- Graphics ---
            case "__sharpie_draw":
                context.Emit("DRAW r1, r2, r3, r4");
                return true;
            case "__sharpie_cls":
                context.Emit("CLS r1");
                return true;
            case "__sharpie_hard_cls":
                context.Emit("ALT CLS r1");
                return true;
            case "__sharpie_cam":
                context.Emit("CAM r1, r2");
                return true;
            case "__sharpie_set_cam":
                context.Emit("ALT CAM r1, r2");
                return true;
            case "__sharpie_swc":
                context.Emit("SWC r1, r2");
                return true;

            // --- Hardware Data (Returns to r0) ---
            case "__sharpie_input":
                context.Emit("INPUT r1, r0"); // Put output directly in r0
                return true;
            case "__sharpie_col":
                context.Emit("COL r1, r0"); // Put collision result in r0
                return true;
            case "__sharpie_oam_tag":
                context.Emit("OAMTAG r1, r0");
                return true;
            case "__sharpie_get_oam":
                context.Emit("GETOAM r0");
                return true;
            case "__sharpie_set_oam":
                context.Emit("SETOAM r1");
                return true;
            case "__sharpie_random":
                context.Emit("ALT RND r0, r1");
                return true;

            // --- Audio ---
            case "__sharpie_play_note":
                context.Emit("PLAY r1, r2, r3");
                return true;
            case "__sharpie_play_song":
                context.Emit("SONG r1");
                return true;
            case "__sharpie_stop":
                context.Emit("STOP r1");
                return true;
            case "__sharpie_mute":
                context.Emit("MUTE");
                return true;
            case "__sharpie_hard_mute":
                context.Emit("ALT MUTE");
                return true;

            // --- System ---
            case "__sharpie_vblnk":
                context.Emit("VBLNK");
                return true;
            case "__sharpie_bank":
                context.Emit("BANK r1");
                return true;
            case "__sharpie_save":
                context.Emit("SAVE");
                return true;
            case "__sharpie_append_save":
                context.Emit("ALT SAVE");
                return true;
            case "__sharpie_halt":
                context.Emit("HALT");
                return true;
        }

        return false;
    }
}
