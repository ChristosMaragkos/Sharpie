using CommandLine;

namespace Sharpie.Cli.Options;

public class CliOptions
{
    [Value(
        0,
        Required = true,
        MetaName = "inputs",
        HelpText = "Input source file(s) (.c or .asm)."
    )]
    public IEnumerable<string> Inputs { get; set; } = [];

    [Option('o', "output", Required = false, HelpText = "The output file path.")]
    public string? Output { get; set; }

    // C Compiler flags

    [Option('O', "optimize", Required = false, HelpText = "Enable C compiler optimizations.")]
    public bool Optimize { get; set; }

    [Option(
        'S',
        "asm-only",
        Required = false,
        HelpText = "Stop after C compilation and emit a file containing Assembly code."
    )]
    public bool StopAtAsm { get; set; }

    // Assembler flags

    [Option('f', "firmware", HelpText = "Assemble as raw firmware (no cartridge header)")]
    public bool IsFirmware { get; set; }

    [Option('t', "title", Default = "Untitled", HelpText = "The ROM title")]
    public string Title { get; set; } = "Untitled";

    [Option('a', "author", Default = "Anonymous", HelpText = "The ROM author")]
    public string Author { get; set; } = "Anonymous";
}
