namespace Sharpie.CCompiler;

public class Instruction
{
    public string OriginalText { get; set; } = "";
    public string Mnemonic { get; set; } = "";
    public string Arg1 { get; set; } = "";
    public string Arg2 { get; set; } = "";
    public string Arg3 { get; set; } = "";
    public string Arg4 { get; set; } = "";

    public bool IsLabel => OriginalText.EndsWith(":");
    public bool IsDirective => Mnemonic.StartsWith(".");
    public bool IsComment => OriginalText.StartsWith(";");

    // Reconstruct the instruction from its parts (useful for when the optimizer modifies it!)
    public void RebuildText()
    {
        if (IsLabel)
            return;

        var parts = new List<string> { Mnemonic };
        if (!string.IsNullOrEmpty(Arg1))
            parts.Add(Arg1);
        if (!string.IsNullOrEmpty(Arg2))
            parts.Add(Arg2);
        if (!string.IsNullOrEmpty(Arg3))
            parts.Add(Arg3);
        if (!string.IsNullOrEmpty(Arg4))
            parts.Add(Arg4);

        OriginalText = parts[0] + " " + string.Join(", ", parts.Skip(1));
    }

    public static Instruction Parse(string asm)
    {
        var inst = new Instruction { OriginalText = asm };

        if (!asm.EndsWith(":") && !asm.StartsWith(";") && !string.IsNullOrWhiteSpace(asm))
        {
            var codePart = asm.Split(';')[0].Trim();
            var tokens = codePart.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length > 0)
            {
                int argStartIndex = 1;
                if (tokens[0] == "ALT" && tokens.Length > 1)
                {
                    inst.Mnemonic = "ALT " + tokens[1];
                    argStartIndex = 2;
                }
                else
                {
                    inst.Mnemonic = tokens[0];
                }

                if (tokens.Length > argStartIndex)
                    inst.Arg1 = tokens[argStartIndex];
                if (tokens.Length > argStartIndex + 1)
                    inst.Arg2 = tokens[argStartIndex + 1];
                if (tokens.Length > argStartIndex + 2)
                    inst.Arg3 = tokens[argStartIndex + 2];
                if (tokens.Length > argStartIndex + 3)
                    inst.Arg4 = tokens[argStartIndex + 3];
            }
        }
        return inst;
    }

    public override string ToString() => OriginalText;
}
