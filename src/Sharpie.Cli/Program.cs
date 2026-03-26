using System.Diagnostics;
using CommandLine;
using Sharpie.Assembler;
using Sharpie.Assembler.Utilities;
using Sharpie.CCompiler;
using Sharpie.Cli.Options;

namespace Sharpie.Cli;

class Program
{
    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<CliOptions>(args).MapResult(RunPipeline, _ => 1);
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
                Console.WriteLine($"[CC] Compiling {inputList.Count} source files...");

                assemblySource = SharpieCC.Compile(options.Inputs, options.Optimize);

                if (options.StopAtAsm)
                {
                    var outPath = options.Output ?? Path.ChangeExtension(inputList[0], ".asm");
                    File.WriteAllText(outPath, assemblySource);
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
