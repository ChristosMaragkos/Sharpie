namespace Sharpie.Cli.Options;

public class CliOptions
{
    public IEnumerable<string> Inputs { get; set; } = [];

    public string? Output { get; set; }

    // C Compiler flags

    public bool Optimize { get; set; }

    public bool StopAtAsm { get; set; }

    // Assembler flags

    public bool IsFirmware { get; set; }

    public string Title { get; set; } = "Untitled";

    public string Author { get; set; } = "Anonymous";
}
