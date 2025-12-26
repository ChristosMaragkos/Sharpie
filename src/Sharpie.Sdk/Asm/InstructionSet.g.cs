// auto-generated
namespace Sharpie.Sdk.Asm;

public static class InstructionSet
{
    private static Dictionary<
        string,
        (int Length, int Hex, int RequiredWords, bool IsFamily)
    > OpcodeTable = new()
    {
        { "NOP", (1, 0, 0, false) },
        { "MOV", (2, 1, 2, false) },
        { "LDM", (3, 16, 2, true) },
        { "LDI", (3, 32, 2, true) },
        { "STM", (3, 48, 2, true) },
        { "ADD", (2, 64, 2, false) },
        { "SUB", (2, 65, 2, false) },
        { "MUL", (2, 66, 2, false) },
        { "DIV", (2, 67, 2, false) },
        { "MOD", (2, 68, 2, false) },
        { "AND", (2, 69, 2, false) },
        { "OR", (2, 70, 2, false) },
        { "XOR", (2, 71, 2, false) },
        { "SHL", (2, 72, 2, false) },
        { "SHR", (2, 73, 2, false) },
        { "CMP", (2, 74, 2, false) },
        { "ADC", (2, 75, 2, false) },
        { "INC", (2, 80, 1, false) },
        { "DEC", (2, 81, 1, false) },
        { "NOT", (2, 82, 1, false) },
        { "NEG", (2, 83, 1, false) },
        { "IADD", (4, 96, 2, false) },
        { "ISUB", (4, 97, 2, false) },
        { "IMUL", (4, 98, 2, false) },
        { "IDIV", (4, 99, 2, false) },
        { "IMOD", (4, 100, 2, false) },
        { "IAND", (4, 101, 2, false) },
        { "IOR", (4, 102, 2, false) },
        { "IXOR", (4, 103, 2, false) },
        { "DINC", (3, 104, 1, false) },
        { "DDEC", (3, 105, 1, false) },
        { "DADD", (4, 106, 2, false) },
        { "DSUB", (4, 107, 2, false) },
        { "DMOV", (4, 108, 2, false) },
        { "DSET", (5, 109, 2, false) },
        { "JMP", (3, 112, 1, false) },
        { "JEQ", (3, 113, 1, false) },
        { "JNE", (3, 114, 1, false) },
        { "JGT", (3, 115, 1, false) },
        { "JLT", (3, 116, 1, false) },
        { "CALL", (3, 117, 1, false) },
        { "RET", (1, 118, 0, false) },
        { "PUSH", (2, 119, 1, false) },
        { "POP", (2, 120, 1, false) },
        { "RND", (4, 128, 2, true) },
        { "SONG", (1, 160, 1, true) },
        { "DRAW", (3, 240, 3, false) },
        { "CLS", (2, 241, 1, false) },
        { "VBLNK", (1, 242, 0, false) },
        { "PLAY", (4, 243, 2, false) },
        { "STOP", (2, 244, 1, false) },
        { "INPUT", (2, 245, 2, false) },
        { "TEXT", (4, 247, 3, false) },
        { "ATTR", (2, 248, 3, false) },
        { "SWC", (2, 249, 2, false) },
        { "FLPH", (2, 250, 1, false) },
        { "FLPV", (2, 251, 1, false) },
        { "MUTE", (1, 252, 0, false) },
        { "PREFIX", (1, 254, 0, false) },
        { "HALT", (1, 255, 0, false) },
    };

    public static int? GetOpcodeLength(string name) =>
        OpcodeTable.ContainsKey(name) ? OpcodeTable[name].Length : null;

    public static int? GetOpcodeHex(string name) =>
        OpcodeTable.ContainsKey(name) ? OpcodeTable[name].Hex : null;

    public static int? GetOpcodeWords(string name) =>
        OpcodeTable.ContainsKey(name) ? OpcodeTable[name].RequiredWords : null;

    public static bool? IsOpcodeFamily(string name) =>
        OpcodeTable.ContainsKey(name) ? OpcodeTable[name].IsFamily : null;

    public static bool IsValidOpcode(string name) => OpcodeTable.ContainsKey(name);
}
