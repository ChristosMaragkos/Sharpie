using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var json = File.ReadAllText("./tools/Sharpie.Generator/res/opcodes.json");
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
File.WriteAllText("./tools/Sharpie.Sdk/Asm/InstructionSet.g.cs", sb.ToString());
Console.WriteLine("Assembler opcode table generated successfuly. Great success!");

sb.Clear();
sb.AppendLine("| Opcode | Mnemonic | Arguments | Length | Description | ALT Prefix Function |");
sb.AppendLine("| :---: | :---: | :---: | :---: | :--- | :--- |");
foreach (var op in ops)
{
    var hexStr = op.Family ? $"0x{op.IntHex:X1}n" : $"0x{op.IntHex:X2}";
    var formattedPattern = "-";

    if (!string.IsNullOrWhiteSpace(op.Pattern))
    {
        formattedPattern = "";
        for (int i = 0; i < op.Pattern.Length; i++)
        {
            if (i == op.Pattern.Length - 1)
                formattedPattern += op.Pattern[i];
            else
                formattedPattern += $"{op.Pattern[i]}, ";
        }
    }

    sb.AppendLine(
        $"| `{hexStr}` | **{op.Name}** | `{formattedPattern}` | {op.Len} | {op.Desc} | {op.Alt} |"
    );
}
File.WriteAllText("./tools/Sharpie.Sdk/docs/ISA_REFERENCE.md", sb.ToString());
Console.WriteLine("Assembly reference document generated. Long live the Empire.");

var nvimOps = string.Join(" ", ops.Select(o => o.Name.ToUpper() + " " + o.Name.ToLower()));
sb.Clear();

sb.AppendLine("\" Auto-generated Sharpie Syntax");
sb.AppendLine("if exists(\"b:current_syntax\") | finish | endif");

sb.AppendLine($"syn keyword sharpieOpcode {nvimOps}");

sb.AppendLine(@"syn match sharpieRegister /\<[rR]\([0-9]\|1[0-5]\)\>/");
sb.AppendLine(@"syn match sharpieHex /\$[0-9A-Fa-f]\+/");
sb.AppendLine(@"syn match sharpieNote /#[A-Ga-g][#Bb]\?\d\+/");
sb.AppendLine(@"syn match sharpieComment /;.*/");
sb.AppendLine(@"syn match sharpieLabel /\<[A-Za-z_][A-Za-z0-9_]*:/");
sb.AppendLine(@"syn match sharpieNumber /\(\$\|0x\)[0-9A-Fa-f]\+/");
sb.AppendLine(@"syn match sharpieNumber /0b[01]\+/");
sb.AppendLine(@"syn match sharpieNumber /\<\d\+\>/");
sb.AppendLine(@"syn match sharpieDirective /\.[A-Za-z_]\+/");

sb.AppendLine(@"syn region sharpieString start=/\""/ end=/\""/ ");

sb.AppendLine("let b:current_syntax = \"sharpie\"");

sb.AppendLine("hi def link sharpieOpcode Statement");
sb.AppendLine("hi def link sharpieRegister Type");
sb.AppendLine("hi def link sharpieHex Constant");
sb.AppendLine("hi def link sharpieNote Special");
sb.AppendLine("hi def link sharpieComment Comment");
sb.AppendLine("hi def link sharpieLabel Function");
sb.AppendLine("hi def link sharpieNumber Number");
sb.AppendLine("hi def link sharpieDirective PreProc");
sb.AppendLine(@"hi def link sharpieString String");

File.WriteAllText("./tools/editor-support/nvim/syntax/sharpie.vim", sb.ToString());
Console.WriteLine("Neovim syntax generated. I'm running out of clever things to write.");

var vsOps = string.Join("|", ops.Select(o => o.Name));
string vscodeSyntax =
    $@"{{
  ""$schema"": ""https://raw.githubusercontent.com/martinring/tmlanguage/master/tmlanguage.json"",
  ""name"": ""Sharpie Assembly"",
  ""patterns"": [
    {{ ""name"": ""variable.other.enum.sharpie"", ""match"": ""\\b[A-Za-z_][A-Za-z0-9_]*::[A-Za-z_][A-Za-z0-9_]*\\b""
    {{ ""name"": ""comment.line.semicolon.sharpie"", ""match"": "";.*"" }},
    {{ ""name"": ""keyword.control.sharpie"", ""match"": ""\\b({vsOps})\\b"" }},
    {{ ""name"": ""variable.parameter.register.sharpie"", ""match"": ""\\b[rR]([0-9]|1[0-5])\\b"" }},
    {{ ""name"": ""constant.numeric.hex.sharpie"", ""match"": ""(\\$|0x)[0-9A-Fa-f]+\\b"" }},
    {{ ""name"": ""entity.name.function.label.sharpie"", ""match"": ""^[A-Za-z_][A-Za-z0-9_]*:"" }},
    {{ ""name"": ""keyword.other.directive.sharpie"", ""match"": ""\\.[A-Za-z_]+\\b"" }}
    {{ ""name"": ""string.quoted.double.sharpie"", ""begin"": ""\\\"""", ""end"": ""\\\"""" }},
    {{ ""name"": ""constant.character.sharpie"", ""match"": ""'[^']'"" }},
  ],
  ""scopeName"": ""source.sharpie""
}}";

File.WriteAllText("./tools/editor-support/vscode/sharpie.tmLanguage.json", vscodeSyntax);

var vscodePackage =
    $@"{{
  ""name"": ""sharpie-lang"",
  ""displayName"": ""Sharpie Assembly"",
  ""description"": ""Syntax highlighting for the Sharpie Fantasy Console"",
  ""version"": ""0.1.0"",
  ""engines"": {{
    ""vscode"": ""^1.50.0""
  }},
  ""categories"": [ ""Programming Languages"" ],
  ""contributes"": {{
    ""languages"": [{{
      ""id"": ""sharpie"",
      ""aliases"": [""Sharpie Assembly"", ""sharpie""],
      ""extensions"": ["".asm""],
      ""configuration"": ""./language-configuration.json""
    }}],
    ""grammars"": [{{
      ""language"": ""sharpie"",
      ""scopeName"": ""source.sharpie"",
      ""path"": ""./sharpie.tmLanguage.json""
    }}]
  }}
}}"; // holy mess of double quotes

File.WriteAllText("./tools/editor-support/vscode/package.json", vscodePackage);

var vscodeConfig =
    @"{
  ""comments"": {
    ""lineComment"": "";""
  },
  ""brackets"": [
    [""["", ""]""],
    [""("", "")""]
  ],
  ""autoClosingPairs"": [
    { ""open"": ""\"""", ""close"": ""\"""" },
    { ""open"": ""'"", ""close"": ""'"" }
  ]
}";

File.WriteAllText("./tools/editor-support/vscode/language-configuration.json", vscodeConfig);
Console.WriteLine("VS Code grammar generated. I think.");

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

    public string Desc { get; set; }
    public string Alt { get; set; }

    [JsonConstructor]
    protected Opcode(
        string hex,
        string name,
        int len,
        string? logic,
        bool family,
        int words,
        string pattern,
        string desc,
        string alt
    )
    {
        Hex = hex;
        Name = name;
        Len = len;
        Logic = logic;
        Family = family;
        Words = words;
        Pattern = pattern;
        Desc = desc;
        Alt = alt;
    }
}
