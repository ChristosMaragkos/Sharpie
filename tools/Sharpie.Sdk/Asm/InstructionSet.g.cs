// auto-generated
namespace Sharpie.Sdk.Asm;

public static class InstructionSet
{

    private static Dictionary<string, (int Length, int Hex, int RequiredWords, bool IsFamily, string Pattern)> OpcodeTable = new()
    {
        { "NOP", (1, 0, 0, false, "") },
        { "MOV", (2, 1, 2, false, "RR") },
        { "LDM", (4, 16, 2, false, "RW") },
        { "LDP", (2, 17, 2, false, "RR") },
        { "LDI", (3, 32, 2, true, "RW") },
        { "STM", (3, 48, 2, true, "RW") },
        { "ADD", (2, 64, 2, false, "RR") },
        { "SUB", (2, 65, 2, false, "RR") },
        { "MUL", (2, 66, 2, false, "RR") },
        { "DIV", (2, 67, 2, false, "RR") },
        { "MOD", (2, 68, 2, false, "RR") },
        { "AND", (2, 69, 2, false, "RR") },
        { "OR", (2, 70, 2, false, "RR") },
        { "XOR", (2, 71, 2, false, "RR") },
        { "SHL", (2, 72, 2, false, "RR") },
        { "SHR", (2, 73, 2, false, "RR") },
        { "CMP", (2, 74, 2, false, "RR") },
        { "ADC", (2, 75, 2, false, "RR") },
        { "INC", (2, 80, 1, false, "R") },
        { "DEC", (2, 81, 1, false, "R") },
        { "NOT", (2, 82, 1, false, "R") },
        { "NEG", (2, 83, 1, false, "R") },
        { "IADD", (3, 96, 2, false, "RB") },
        { "ISUB", (3, 97, 2, false, "RB") },
        { "IMUL", (3, 98, 2, false, "RB") },
        { "IDIV", (3, 99, 2, false, "RB") },
        { "IMOD", (3, 100, 2, false, "RB") },
        { "IAND", (3, 101, 2, false, "RB") },
        { "IOR", (3, 102, 2, false, "RB") },
        { "IXOR", (3, 103, 2, false, "RB") },
        { "ICMP", (3, 104, 2, false, "RB") },
        { "DINC", (2, 105, 1, false, "R") },
        { "DDEC", (2, 106, 1, false, "R") },
        { "JMP", (3, 112, 1, false, "W") },
        { "JEQ", (3, 113, 1, false, "W") },
        { "JNE", (3, 114, 1, false, "W") },
        { "JGT", (3, 115, 1, false, "W") },
        { "JLT", (3, 116, 1, false, "W") },
        { "JGE", (3, 117, 1, false, "W") },
        { "JLE", (3, 118, 1, false, "W") },
        { "CALL", (3, 119, 1, false, "W") },
        { "RET", (1, 120, 0, false, "") },
        { "PUSH", (2, 121, 1, false, "R") },
        { "POP", (2, 122, 1, false, "R") },
        { "OUT_R", (2, 123, 1, false, "R") },
        { "OUT_B", (2, 124, 1, false, "B") },
        { "OUT_W", (3, 125, 1, false, "W") },
        { "RND", (3, 128, 2, true, "RW") },
        { "SONG", (1, 160, 1, true, "R") },
        { "SETCRS", (3, 192, 2, false, "BB") },
        { "DRAW", (3, 208, 5, true, "RRRRR") },
        { "INSTR", (3, 224, 3, true, "RBB") },
        { "TAG", (2, 240, 2, false, "RR") },
        { "CLS", (2, 241, 1, false, "R") },
        { "VBLNK", (1, 242, 0, false, "") },
        { "PLAY", (3, 243, 3, false, "RRR") },
        { "STOP", (2, 244, 1, false, "R") },
        { "INPUT", (2, 245, 2, false, "RR") },
        { "TEXT", (2, 247, 1, false, "B") },
        { "ATTR", (2, 248, 1, false, "B") },
        { "SWC", (2, 249, 2, false, "RR") },
        { "MUTE", (1, 252, 0, false, "") },
        { "COL", (2, 253, 2, false, "RR") },
        { "ALT", (1, 254, 0, false, "") },
        { "HALT", (1, 255, 0, false, "") },
    };

    public static int GetOpcodeLength(string name)
        => OpcodeTable.ContainsKey(name) ? OpcodeTable[name].Length : throw new AssemblySyntaxException($"Unexpected token: {name}");

    public static int GetOpcodeHex(string name)
        => OpcodeTable.ContainsKey(name) ? OpcodeTable[name].Hex : throw new AssemblySyntaxException($"Unexpected token: {name}");

    public static int GetOpcodeWords(string name)
        => OpcodeTable.ContainsKey(name) ? OpcodeTable[name].RequiredWords : throw new AssemblySyntaxException($"Unexpected token: {name}");

    public static bool IsOpcodeFamily(string name)
        => OpcodeTable.ContainsKey(name) ? OpcodeTable[name].IsFamily : throw new AssemblySyntaxException($"Unexpected token: {name}");

    public static string GetOpcodePattern(string name)
        => OpcodeTable.ContainsKey(name) ? OpcodeTable[name].Pattern : throw new AssemblySyntaxException($"Unexpected token: {name}");

    public static bool IsValidOpcode(string name)
        => OpcodeTable.ContainsKey(name);
}
