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
sb.AppendLine("namespace Sharpie.Core;");
sb.AppendLine("public partial class Cpu {");
sb.AppendLine("    private void ExecuteOpcode(byte opcode, out ushort pcDelta) {");
sb.AppendLine("        pcDelta = 0;");
sb.AppendLine("        switch (opcode) {");

foreach (var op in ops)
{
    var start = Convert.ToInt32(op.Hex, 16);
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
File.WriteAllText("./src/Sharpie.Core/Cpu.Ops.g.cs", sb.ToString());
Console.WriteLine("Opcode switch generated successfully. Sanity saved.");

return;

class Opcode
{
    public int IntHex => int.Parse(Hex);
    public string Hex { get; set; }
    public string Name { get; set; }
    public int Len { get; set; }
    public bool Family { get; set; }

    public string? Logic { get; set; }

    [JsonConstructor]
    protected Opcode(string hex, string name, int len, string? logic, bool family)
    {
        Hex = hex;
        Name = name;
        Len = len;
        Logic = logic;
        Family = family;
    }
}

