using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var json = File.ReadAllText("./src/Sharpie.Generator/res/opcodes.json");
JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var ops =
    JsonSerializer.Deserialize<List<Opcode>>(json, options)
    ?? throw new JsonException("Bad json. BAD!");

var sb = new StringBuilder();
sb.AppendLine("// auto-generated");
sb.AppendLine("namespace Sharpie.Core.Hardware;");
sb.AppendLine("internal partial class Cpu {");
sb.AppendLine("    private void ExecuteOpcode(byte opcode, out ushort pcDelta) {");
sb.AppendLine("        pcDelta = 0;");
sb.AppendLine("        switch (opcode) {");

foreach (var op in ops)
{
    var start = op.IntHex;
    var end = op.Family ? start + 15 : start;

    if (start == end)
        sb.AppendLine($"            case 0x{start:X2}: //{op.Name}");
    else
        sb.AppendLine($"            case >= 0x{start:X2} and <= 0x{end:X2}:");

    sb.AppendLine($"                pcDelta = {op.Len};");
    if (op.Logic == null)
    {
        sb.AppendLine($"                Execute_{op.Name}(opcode, ref pcDelta);");
    }
    else if (op.Logic.Length > 0)
    {
        sb.AppendLine($"                {op.Logic}");
    }

    sb.AppendLine("                break;");
    sb.AppendLine();
}

sb.AppendLine("            default:");
sb.AppendLine("                Console.WriteLine($\"Unknown Opcode: 0x{opcode:X2}\");");
sb.AppendLine("                IsHalted = true;");
sb.AppendLine("                pcDelta = 1;");
sb.AppendLine("                break;");

sb.AppendLine("        }");
sb.AppendLine("    }");
sb.AppendLine();

foreach (var op in ops)
{
    if (op.Logic != null)
        continue;
    sb.AppendLine($"    private partial void Execute_{op.Name}(byte opcode, ref ushort pcDelta);");
}

sb.AppendLine("}");
File.WriteAllText("./src/Sharpie.Core/Hardware/Cpu.Ops.g.cs", sb.ToString());
Console.WriteLine("Opcode switch generated successfully. Sanity saved.");

sb.Clear();
sb.AppendLine("// auto-generated");
sb.AppendLine("namespace Sharpie.Sdk.Asm;");
sb.AppendLine("");
sb.AppendLine("public static class InstructionSet");
sb.AppendLine("{");
sb.AppendLine("");
sb.AppendLine(
    "    private static Dictionary<string, (int Length, int Hex, int RequiredWords, bool IsFamily, string Pattern)> OpcodeTable = new()"
);
sb.AppendLine("    {");

foreach (var op in ops)
{
    sb.AppendLine(
        $"        {{ \"{op.Name}\", ({op.Len}, {op.IntHex}, {op.Words}, {op.Family.ToString().ToLower()}, {(string.IsNullOrWhiteSpace(op.Pattern) ? "\"\"" : "\"" + op.Pattern + "\"")}) }},"
    );
}
sb.AppendLine("    };");
sb.AppendLine("");
sb.AppendLine("    public static int GetOpcodeLength(string name)");
sb.AppendLine(
    "        => OpcodeTable.ContainsKey(name) ? OpcodeTable[name].Length : throw new AssemblySyntaxException($\"Unexpected token: {name}\");"
);
sb.AppendLine("");
sb.AppendLine("    public static int GetOpcodeHex(string name)");
sb.AppendLine(
    "        => OpcodeTable.ContainsKey(name) ? OpcodeTable[name].Hex : throw new AssemblySyntaxException($\"Unexpected token: {name}\");"
);
sb.AppendLine("");
sb.AppendLine("    public static int GetOpcodeWords(string name)");
sb.AppendLine(
    "        => OpcodeTable.ContainsKey(name) ? OpcodeTable[name].RequiredWords : throw new AssemblySyntaxException($\"Unexpected token: {name}\");"
);
sb.AppendLine("");
sb.AppendLine("    public static bool IsOpcodeFamily(string name)");
sb.AppendLine(
    "        => OpcodeTable.ContainsKey(name) ? OpcodeTable[name].IsFamily : throw new AssemblySyntaxException($\"Unexpected token: {name}\");"
);
sb.AppendLine("");
sb.AppendLine("    public static string GetOpcodePattern(string name)");
sb.AppendLine(
    "        => OpcodeTable.ContainsKey(name) ? OpcodeTable[name].Pattern : throw new AssemblySyntaxException($\"Unexpected token: {name}\");"
);
sb.AppendLine("");
sb.AppendLine("    public static bool IsValidOpcode(string name)");
sb.AppendLine("        => OpcodeTable.ContainsKey(name);");
sb.AppendLine("}");
File.WriteAllText("./src/Sharpie.Sdk/Asm/InstructionSet.g.cs", sb.ToString());
Console.WriteLine("Assembler opcode table generated successfuly. Great success!");

return;

class Opcode
{
    public int IntHex => Convert.ToInt32(Hex, 16);
    public string Hex { get; set; }
    public string Name { get; set; }
    public int Len { get; set; }
    public bool Family { get; set; }

    public string? Logic { get; set; }

    public int Words { get; set; }
    public string Pattern { get; set; }

    [JsonConstructor]
    protected Opcode(
        string hex,
        string name,
        int len,
        string? logic,
        bool family,
        int words,
        string pattern
    )
    {
        Hex = hex;
        Name = name;
        Len = len;
        Logic = logic;
        Family = family;
        Words = words;
        Pattern = pattern;
    }
}
