using System.Diagnostics;
using Sharpie.Assembler;
using Sharpie.Assembler.Utilities;
using Sharpie.CCompiler;
using Sharpie.Cli.Options;

namespace Sharpie.Cli;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            PrintHelp();
            Environment.Exit(0);
        }

        try
        {
            var options = ParseArgs(args);
            int exitCode = RunPipeline(options);
            Environment.Exit(exitCode);
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Error: {e.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    private static CliOptions ParseArgs(string[] args)
    {
        var options = new CliOptions();
        var inputs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-O":
                case "--optimize":
                    options.Optimize = true;
                    break;
                case "-S":
                case "--asm-only":
                    options.StopAtAsm = true;
                    break;
                case "-OS":
                case "-SO":
                    options.Optimize = true;
                    options.StopAtAsm = true;
                    break;
                case "-f":
                case "--firmware":
                    options.IsFirmware = true;
                    break;
                case "-o":
                case "--output":
                    if (i + 1 < args.Length)
                        options.Output = args[++i];
                    else
                        throw new ArgumentException("Missing value for -o/--output");
                    break;
                case "-t":
                case "--title":
                    if (i + 1 < args.Length)
                        options.Title = args[++i];
                    else
                        throw new ArgumentException("Missing value for -t/--title");
                    break;
                case "-a":
                case "--author":
                    if (i + 1 < args.Length)
                        options.Author = args[++i];
                    else
                        throw new ArgumentException("Missing value for -a/--author");
                    break;
                default:
                    if (args[i].StartsWith('-'))
                        throw new ArgumentException($"Unknown argument: {args[i]}");

                    inputs.Add(args[i]);
                    break;
            }
        }

        if (inputs.Count == 0)
            throw new ArgumentException("No input files specified.");

        options.Inputs = inputs;
        return options;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Sharpie CLI");
        Console.WriteLine("Usage: sharpie <inputs...> [options]");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("  -o, --output <path>    The output file path (.shr, .bin, or .asm).");
        Console.WriteLine("  -O, --optimize         Enable C compiler optimizations.");
        Console.WriteLine(
            "  -S, --asm-only         Stop after C compilation and emit assembly text. Can be combined with -O."
        );
        Console.WriteLine(
            "  -f, --firmware         Assemble as raw firmware (no cartridge header)."
        );
        Console.WriteLine("  -t, --title <name>     The ROM title (default: Untitled).");
        Console.WriteLine("  -a, --author <name>    The ROM author (default: Anonymous).");
        Console.WriteLine("  -h, --help             Show this help screen.");
    }

    private static int RunPipeline(CliOptions options)
    {
        Stopwatch sw = Stopwatch.StartNew();

        try
        {
            var inputList = options.Inputs.ToList();

            foreach (var file in inputList)
            {
                if (!File.Exists(file))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"Input file not found: '{file}'");
                    return 1;
                }
            }

            string firstFileExt = Path.GetExtension(inputList[0]).ToLower();

            if (inputList.Count > 1 && firstFileExt is not ".c")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Multi-file input is only supported for C compilation.");
                Console.ResetColor();
                return 1;
            }
            if (
                options.IsFirmware
                && (options.Title is not "Untitled" || options.Author is not "Anonymous")
            )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(
                    "Metadata flags (-t, -a) are not supported in firmware mode (-f|--firmware)"
                );
                Console.ResetColor();
                return 1;
            }
            if (
                options.StopAtAsm
                && (options.Title is not "Untitled" || options.Author is not "Anonymous")
            )
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Error.WriteLine("Metadata flags (-t, -a) are ignored when using -S.");
                Console.ResetColor();
            }
            if (firstFileExt is ".asm" && (options.Optimize || options.StopAtAsm))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(
                    "Compiler flags (-O, -S) cannot be used when the input is an assembly (.asm) file."
                );
                Console.ResetColor();
                return 1;
            }

            string? assemblySource = null;

            if (firstFileExt is ".c")
            {
                Console.WriteLine($"[CC] Compiling {inputList.Count} source file(s)...");

                assemblySource = SharpieCC.Compile(options.Inputs, options.Optimize);

                if (options.StopAtAsm)
                {
                    var outPath = options.Output ?? Path.ChangeExtension(inputList[0], ".asm");
                    File.WriteAllText(outPath, assemblySource);
                    sw.Stop();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(
                        $"Compilation successful in {sw.ElapsedMilliseconds / 1000f} seconds ({outPath})"
                    );
                    Console.ResetColor();
                    return 0;
                }
            }

            byte[] machineCode;
            if (assemblySource != null)
            {
                Console.WriteLine($"[ASM] Assembling emitted code...");
                var assembler = new SharpieAssembler(options.IsFirmware);
                machineCode = assembler.AssembleFromText(assemblySource);
            }
            else if (firstFileExt is ".asm")
            {
                var asmInputFile = inputList.First();
                Console.WriteLine($"[ASM] Assembling {asmInputFile}...");
                var assembler = new SharpieAssembler(options.IsFirmware);
                machineCode = assembler.AssembleFromFile(asmInputFile);
            }
            else
            {
                throw new Exception($"Unsupported file type: {firstFileExt}");
            }

            var exporter = new SharpieExporter { Title = options.Title, Author = options.Author };

            byte[] finalBinary = exporter.CreateCartridge(machineCode, options.IsFirmware);
            string finalPath =
                options.Output
                ?? Path.ChangeExtension(
                    options.Inputs.First(),
                    options.IsFirmware ? ".bin" : ".shr"
                );

            File.WriteAllBytes(finalPath, finalBinary);

            sw.Stop();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                $"Build succeeded in {sw.ElapsedMilliseconds / 1000f} seconds: {finalPath}"
            );
            Console.ResetColor();

            return 0;
        }
        catch (Exception e)
        {
            sw.Stop();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(
                $"\nBuild failed in {sw.ElapsedMilliseconds / 1000f} seconds with error: {e.Message}"
            );
            Console.ResetColor();
            return 1;
        }
    }
}
