public struct TokenLine
{
    public string? Opcode { get; set; }
    public string[]? Args { get; set; }
    public int? SourceLine { get; set; }

    public TokenLine()
    {
        (Opcode, Args, SourceLine) = (null, null, null);
    }

    /// Returns whether all properties are null, effectively determining if a non-empty line was processed.
    public bool ArePropertiesNull() => Opcode == null && Args == null;

    public override string ToString()
    {
        var str = $"Token line: Opcode = {Opcode} | Source Line = {SourceLine} |";

        if (Args == null)
            str += "Args = null";
        else
            for (int i = 0; i < Args.Length; i++)
                str += $"\nArgs[{i}] = {Args[i]}";

        return str;
    }
}
