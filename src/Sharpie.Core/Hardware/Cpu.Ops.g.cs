// auto-generated
namespace Sharpie.Core.Hardware;
internal partial class Cpu {
    private void ExecuteOpcode(byte opcode, out ushort pcDelta) {
        pcDelta = 0;
        switch (opcode) {
            case 0x00: //NOP
                pcDelta = 1;
                break;

            case 0x01: //MOV
                pcDelta = 2;
                Execute_MOV(opcode, ref pcDelta);
                break;

            case 0x10: //LDM
                pcDelta = 4;
                Execute_LDM(opcode, ref pcDelta);
                break;

            case 0x11: //LDP
                pcDelta = 2;
                Execute_LDP(opcode, ref pcDelta);
                break;

            case >= 0x20 and <= 0x2F:
                pcDelta = 3;
                Execute_LDI(opcode, ref pcDelta);
                break;

            case >= 0x30 and <= 0x3F:
                pcDelta = 3;
                Execute_STM(opcode, ref pcDelta);
                break;

            case 0x12: //STP
                pcDelta = 2;
                Execute_STP(opcode, ref pcDelta);
                break;

            case 0x13: //STA
                pcDelta = 2;
                Execute_STA(opcode, ref pcDelta);
                break;

            case 0x40: //ADD
                pcDelta = 2;
                Execute_ADD(opcode, ref pcDelta);
                break;

            case 0x41: //SUB
                pcDelta = 2;
                Execute_SUB(opcode, ref pcDelta);
                break;

            case 0x42: //MUL
                pcDelta = 2;
                Execute_MUL(opcode, ref pcDelta);
                break;

            case 0x43: //DIV
                pcDelta = 2;
                Execute_DIV(opcode, ref pcDelta);
                break;

            case 0x44: //MOD
                pcDelta = 2;
                Execute_MOD(opcode, ref pcDelta);
                break;

            case 0x45: //AND
                pcDelta = 2;
                Execute_AND(opcode, ref pcDelta);
                break;

            case 0x46: //OR
                pcDelta = 2;
                Execute_OR(opcode, ref pcDelta);
                break;

            case 0x47: //XOR
                pcDelta = 2;
                Execute_XOR(opcode, ref pcDelta);
                break;

            case 0x48: //SHL
                pcDelta = 2;
                Execute_SHL(opcode, ref pcDelta);
                break;

            case 0x49: //SHR
                pcDelta = 2;
                Execute_SHR(opcode, ref pcDelta);
                break;

            case 0x4A: //CMP
                pcDelta = 2;
                Execute_CMP(opcode, ref pcDelta);
                break;

            case 0x4B: //ADC
                pcDelta = 2;
                Execute_ADC(opcode, ref pcDelta);
                break;

            case 0x50: //INC
                pcDelta = 2;
                Execute_INC(opcode, ref pcDelta);
                break;

            case 0x51: //DEC
                pcDelta = 2;
                Execute_DEC(opcode, ref pcDelta);
                break;

            case 0x52: //NOT
                pcDelta = 2;
                Execute_NOT(opcode, ref pcDelta);
                break;

            case 0x53: //NEG
                pcDelta = 2;
                Execute_NEG(opcode, ref pcDelta);
                break;

            case 0x60: //IADD
                pcDelta = 3;
                Execute_IADD(opcode, ref pcDelta);
                break;

            case 0x61: //ISUB
                pcDelta = 3;
                Execute_ISUB(opcode, ref pcDelta);
                break;

            case 0x62: //IMUL
                pcDelta = 3;
                Execute_IMUL(opcode, ref pcDelta);
                break;

            case 0x63: //IDIV
                pcDelta = 3;
                Execute_IDIV(opcode, ref pcDelta);
                break;

            case 0x64: //IMOD
                pcDelta = 3;
                Execute_IMOD(opcode, ref pcDelta);
                break;

            case 0x65: //IAND
                pcDelta = 3;
                Execute_IAND(opcode, ref pcDelta);
                break;

            case 0x66: //IOR
                pcDelta = 3;
                Execute_IOR(opcode, ref pcDelta);
                break;

            case 0x67: //IXOR
                pcDelta = 3;
                Execute_IXOR(opcode, ref pcDelta);
                break;

            case 0x68: //ICMP
                pcDelta = 3;
                Execute_ICMP(opcode, ref pcDelta);
                break;

            case 0x69: //DINC
                pcDelta = 2;
                Execute_DINC(opcode, ref pcDelta);
                break;

            case 0x6A: //DDEC
                pcDelta = 2;
                Execute_DDEC(opcode, ref pcDelta);
                break;

            case 0x70: //JMP
                pcDelta = 3;
                Execute_JMP(opcode, ref pcDelta);
                break;

            case 0x71: //JEQ
                pcDelta = 3;
                Execute_JEQ(opcode, ref pcDelta);
                break;

            case 0x72: //JNE
                pcDelta = 3;
                Execute_JNE(opcode, ref pcDelta);
                break;

            case 0x73: //JGT
                pcDelta = 3;
                Execute_JGT(opcode, ref pcDelta);
                break;

            case 0x74: //JLT
                pcDelta = 3;
                Execute_JLT(opcode, ref pcDelta);
                break;

            case 0x75: //JGE
                pcDelta = 3;
                Execute_JGE(opcode, ref pcDelta);
                break;

            case 0x76: //JLE
                pcDelta = 3;
                Execute_JLE(opcode, ref pcDelta);
                break;

            case 0x77: //CALL
                pcDelta = 3;
                Execute_CALL(opcode, ref pcDelta);
                break;

            case 0x78: //RET
                pcDelta = 1;
                Execute_RET(opcode, ref pcDelta);
                break;

            case 0x79: //PUSH
                pcDelta = 2;
                Execute_PUSH(opcode, ref pcDelta);
                break;

            case 0x7A: //POP
                pcDelta = 2;
                Execute_POP(opcode, ref pcDelta);
                break;

            case 0x7B: //OUT_R
                pcDelta = 2;
                Execute_OUT_R(opcode, ref pcDelta);
                break;

            case 0x7C: //OUT_B
                pcDelta = 2;
                Execute_OUT_B(opcode, ref pcDelta);
                break;

            case 0x7D: //OUT_W
                pcDelta = 3;
                Execute_OUT_W(opcode, ref pcDelta);
                break;

            case >= 0x80 and <= 0x8F:
                pcDelta = 3;
                Execute_RND(opcode, ref pcDelta);
                break;

            case 0x90: //FLIPR
                pcDelta = 1;
                Execute_FLIPR(opcode, ref pcDelta);
                break;

            case 0x91: //CAM
                pcDelta = 2;
                Execute_CAM(opcode, ref pcDelta);
                break;

            case 0x92: //GETOAM
                pcDelta = 2;
                Execute_GETOAM(opcode, ref pcDelta);
                break;

            case 0x93: //SETOAM
                pcDelta = 2;
                Execute_SETOAM(opcode, ref pcDelta);
                break;

            case 0x94: //GETSEQ
                pcDelta = 2;
                Execute_GETSEQ(opcode, ref pcDelta);
                break;

            case 0x95: //SETSEQ
                pcDelta = 2;
                Execute_SETSEQ(opcode, ref pcDelta);
                break;

            case >= 0xA0 and <= 0xAF:
                pcDelta = 1;
                Execute_SONG(opcode, ref pcDelta);
                break;

            case 0xC0: //SETCRS
                pcDelta = 3;
                Execute_SETCRS(opcode, ref pcDelta);
                break;

            case >= 0xD0 and <= 0xDF:
                pcDelta = 3;
                Execute_DRAW(opcode, ref pcDelta);
                break;

            case >= 0xE0 and <= 0xEF:
                pcDelta = 3;
                Execute_INSTR(opcode, ref pcDelta);
                break;

            case 0xC1: //OAMPOS
                pcDelta = 3;
                Execute_OAMPOS(opcode, ref pcDelta);
                break;

            case 0xC2: //SAVE
                pcDelta = 1;
                Execute_SAVE(opcode, ref pcDelta);
                break;

            case 0xF0: //OAMTAG
                pcDelta = 2;
                Execute_OAMTAG(opcode, ref pcDelta);
                break;

            case 0xF1: //CLS
                pcDelta = 2;
                Execute_CLS(opcode, ref pcDelta);
                break;

            case 0xF2: //VBLNK
                pcDelta = 1;
                Execute_VBLNK(opcode, ref pcDelta);
                break;

            case 0xF3: //PLAY
                pcDelta = 3;
                Execute_PLAY(opcode, ref pcDelta);
                break;

            case 0xF4: //STOP
                pcDelta = 2;
                Execute_STOP(opcode, ref pcDelta);
                break;

            case 0xF5: //INPUT
                pcDelta = 2;
                Execute_INPUT(opcode, ref pcDelta);
                break;

            case 0xF7: //TEXT
                pcDelta = 2;
                Execute_TEXT(opcode, ref pcDelta);
                break;

            case 0xF8: //ATTR
                pcDelta = 2;
                Execute_ATTR(opcode, ref pcDelta);
                break;

            case 0xF9: //SWC
                pcDelta = 2;
                Execute_SWC(opcode, ref pcDelta);
                break;

            case 0xFA: //BANK
                pcDelta = 2;
                Execute_BANK(opcode, ref pcDelta);
                break;

            case 0xFC: //MUTE
                pcDelta = 1;
                Execute_MUTE(opcode, ref pcDelta);
                break;

            case 0xFD: //COL
                pcDelta = 2;
                Execute_COL(opcode, ref pcDelta);
                break;

            case 0xFE: //ALT
                pcDelta = 1;
                Execute_ALT(opcode, ref pcDelta);
                break;

            case 0xFF: //HALT
                pcDelta = 1;
                IsHalted = true;
                break;

            default:
                _mobo.PushDebug($"Unknown Opcode @ {_pc:X4} : 0x{opcode:X2}");
                IsHalted = true;
                pcDelta = 1;
                break;
        }
    }

    private partial void Execute_MOV(byte opcode, ref ushort pcDelta);
    private partial void Execute_LDM(byte opcode, ref ushort pcDelta);
    private partial void Execute_LDP(byte opcode, ref ushort pcDelta);
    private partial void Execute_LDI(byte opcode, ref ushort pcDelta);
    private partial void Execute_STM(byte opcode, ref ushort pcDelta);
    private partial void Execute_STP(byte opcode, ref ushort pcDelta);
    private partial void Execute_STA(byte opcode, ref ushort pcDelta);
    private partial void Execute_ADD(byte opcode, ref ushort pcDelta);
    private partial void Execute_SUB(byte opcode, ref ushort pcDelta);
    private partial void Execute_MUL(byte opcode, ref ushort pcDelta);
    private partial void Execute_DIV(byte opcode, ref ushort pcDelta);
    private partial void Execute_MOD(byte opcode, ref ushort pcDelta);
    private partial void Execute_AND(byte opcode, ref ushort pcDelta);
    private partial void Execute_OR(byte opcode, ref ushort pcDelta);
    private partial void Execute_XOR(byte opcode, ref ushort pcDelta);
    private partial void Execute_SHL(byte opcode, ref ushort pcDelta);
    private partial void Execute_SHR(byte opcode, ref ushort pcDelta);
    private partial void Execute_CMP(byte opcode, ref ushort pcDelta);
    private partial void Execute_ADC(byte opcode, ref ushort pcDelta);
    private partial void Execute_INC(byte opcode, ref ushort pcDelta);
    private partial void Execute_DEC(byte opcode, ref ushort pcDelta);
    private partial void Execute_NOT(byte opcode, ref ushort pcDelta);
    private partial void Execute_NEG(byte opcode, ref ushort pcDelta);
    private partial void Execute_IADD(byte opcode, ref ushort pcDelta);
    private partial void Execute_ISUB(byte opcode, ref ushort pcDelta);
    private partial void Execute_IMUL(byte opcode, ref ushort pcDelta);
    private partial void Execute_IDIV(byte opcode, ref ushort pcDelta);
    private partial void Execute_IMOD(byte opcode, ref ushort pcDelta);
    private partial void Execute_IAND(byte opcode, ref ushort pcDelta);
    private partial void Execute_IOR(byte opcode, ref ushort pcDelta);
    private partial void Execute_IXOR(byte opcode, ref ushort pcDelta);
    private partial void Execute_ICMP(byte opcode, ref ushort pcDelta);
    private partial void Execute_DINC(byte opcode, ref ushort pcDelta);
    private partial void Execute_DDEC(byte opcode, ref ushort pcDelta);
    private partial void Execute_JMP(byte opcode, ref ushort pcDelta);
    private partial void Execute_JEQ(byte opcode, ref ushort pcDelta);
    private partial void Execute_JNE(byte opcode, ref ushort pcDelta);
    private partial void Execute_JGT(byte opcode, ref ushort pcDelta);
    private partial void Execute_JLT(byte opcode, ref ushort pcDelta);
    private partial void Execute_JGE(byte opcode, ref ushort pcDelta);
    private partial void Execute_JLE(byte opcode, ref ushort pcDelta);
    private partial void Execute_CALL(byte opcode, ref ushort pcDelta);
    private partial void Execute_RET(byte opcode, ref ushort pcDelta);
    private partial void Execute_PUSH(byte opcode, ref ushort pcDelta);
    private partial void Execute_POP(byte opcode, ref ushort pcDelta);
    private partial void Execute_OUT_R(byte opcode, ref ushort pcDelta);
    private partial void Execute_OUT_B(byte opcode, ref ushort pcDelta);
    private partial void Execute_OUT_W(byte opcode, ref ushort pcDelta);
    private partial void Execute_RND(byte opcode, ref ushort pcDelta);
    private partial void Execute_FLIPR(byte opcode, ref ushort pcDelta);
    private partial void Execute_CAM(byte opcode, ref ushort pcDelta);
    private partial void Execute_GETOAM(byte opcode, ref ushort pcDelta);
    private partial void Execute_SETOAM(byte opcode, ref ushort pcDelta);
    private partial void Execute_GETSEQ(byte opcode, ref ushort pcDelta);
    private partial void Execute_SETSEQ(byte opcode, ref ushort pcDelta);
    private partial void Execute_SONG(byte opcode, ref ushort pcDelta);
    private partial void Execute_SETCRS(byte opcode, ref ushort pcDelta);
    private partial void Execute_DRAW(byte opcode, ref ushort pcDelta);
    private partial void Execute_INSTR(byte opcode, ref ushort pcDelta);
    private partial void Execute_OAMPOS(byte opcode, ref ushort pcDelta);
    private partial void Execute_SAVE(byte opcode, ref ushort pcDelta);
    private partial void Execute_OAMTAG(byte opcode, ref ushort pcDelta);
    private partial void Execute_CLS(byte opcode, ref ushort pcDelta);
    private partial void Execute_VBLNK(byte opcode, ref ushort pcDelta);
    private partial void Execute_PLAY(byte opcode, ref ushort pcDelta);
    private partial void Execute_STOP(byte opcode, ref ushort pcDelta);
    private partial void Execute_INPUT(byte opcode, ref ushort pcDelta);
    private partial void Execute_TEXT(byte opcode, ref ushort pcDelta);
    private partial void Execute_ATTR(byte opcode, ref ushort pcDelta);
    private partial void Execute_SWC(byte opcode, ref ushort pcDelta);
    private partial void Execute_BANK(byte opcode, ref ushort pcDelta);
    private partial void Execute_MUTE(byte opcode, ref ushort pcDelta);
    private partial void Execute_COL(byte opcode, ref ushort pcDelta);
    private partial void Execute_ALT(byte opcode, ref ushort pcDelta);
}
