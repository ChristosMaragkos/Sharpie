using System.Text;
using Sharpie.CCompiler;
using Sharpie.CCompiler.NativeInterop;

LibClangResolver.Configure();

var inputFiles = new List<string>();
string? outputPath = null;
bool optimize = false;
bool emitAssembly = false;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "-h" || args[i] == "--help")
    {
        PrintHelp();
        return;
    }
    else if (args[i] == "-O")
        optimize = true;
    else if (args[i] == "-S")
        emitAssembly = true;
    else if (args[i] == "-o" && i + 1 < args.Length)
    {
        outputPath = args[++i];
    }
    else if (!args[i].StartsWith("-"))
    {
        inputFiles.Add(args[i]);
    }
    else
    {
        Console.Error.WriteLine($"Unknown argument: {args[i]}");
        Environment.ExitCode = 1;
        return;
    }
}

if (inputFiles.Count == 0)
{
    Console.Error.WriteLine("Error: No input files specified.");
    PrintHelp();
    Environment.ExitCode = 1;
    return;
}

if (string.IsNullOrEmpty(outputPath))
{
    var baseName = Path.GetFileNameWithoutExtension(inputFiles[0]);
    outputPath = emitAssembly ? $"{baseName}.asm" : $"{baseName}.shr";
}

foreach (var file in inputFiles)
{
    if (!File.Exists(file))
    {
        Console.Error.WriteLine($"Error: Input file not found: {file}");
        Environment.ExitCode = 1;
        return;
    }
}

try
{
    var finalAssembly = CompileFiles(inputFiles, optimize);

    if (emitAssembly)
    {
        File.WriteAllText(outputPath, finalAssembly);
        Console.WriteLine($"Wrote Sharpie assembly to {outputPath}");
    }
    else
    {
        var assembler = new Sharpie.Sdk.Asm.Assembler(false);
        var data = assembler.LoadRawAsm(finalAssembly);

        var exporter = new Sharpie.Sdk.Asm.Exporter(
            "sharpie-cc",
            "sharpie-cc",
            outputPath,
            Array.Empty<int>()
        );
        exporter.ExportRom(data, false);
        Console.WriteLine($"Wrote Sharpie cartridge to {outputPath}");
    }
}
catch (TypeInitializationException ex)
{
    Console.Error.WriteLine("ClangSharp initialization failed - libclang not found.");
    Console.Error.WriteLine("Make sure libclang is installed on your system.");
    if (ex.InnerException != null)
        Console.Error.WriteLine($"Details: {ex.InnerException.Message}");
    Environment.ExitCode = 1;
}
catch (DllNotFoundException ex)
{
    Console.Error.WriteLine("Failed to load libclang.");
    Console.Error.WriteLine(
        "Install libclang for your platform or set SHARPIE_LIBCLANG_PATH to a valid library path."
    );
    Console.Error.WriteLine(ex.ToString());
    Environment.ExitCode = 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal Error: {ex.Message}");
    Environment.ExitCode = 1;
}

static string CompileFiles(List<string> inputFiles, bool optimize)
{
    var masterAssembly = new StringBuilder();
    masterAssembly.AppendLine("; ------------------------");
    masterAssembly.AppendLine("; Sharpie C cartridge");
    masterAssembly.AppendLine("; ------------------------");
    masterAssembly.AppendLine(".REGION FIXED");
    masterAssembly.AppendLine("    JMP Main");
    masterAssembly.AppendLine(".ENDREGION");
    using var index = ClangSharp.Interop.CXIndex.Create();

    var clangArgs = new[] { "-std=gnu11", "-target", "msp430" };

    foreach (var file in inputFiles)
    {
        using var tu = ClangSharp.Interop.CXTranslationUnit.Parse(
            index,
            file,
            clangArgs,
            Array.Empty<ClangSharp.Interop.CXUnsavedFile>(),
            ClangSharp.Interop.CXTranslationUnit_Flags.CXTranslationUnit_None
        );

        if (PrintDiagnostics(tu, file))
        {
            throw new Exception("Compilation failed due to frontend errors.");
        }

        var emitter = new SharpieEmitter(optimize);

        masterAssembly.AppendLine($"; ----------------------------------");
        masterAssembly.AppendLine($"; SOURCE: {Path.GetFileName(file)}");
        masterAssembly.AppendLine($"; ----------------------------------");
        masterAssembly.AppendLine(emitter.EmitTranslationUnit(tu.Cursor));
    }

    return masterAssembly.ToString();
}

static bool PrintDiagnostics(ClangSharp.Interop.CXTranslationUnit tu, string filename)
{
    var hasErrors = false;

    for (uint i = 0; i < tu.NumDiagnostics; i++)
    {
        using var diag = tu.GetDiagnostic(i);
        var severity = diag.Severity;
        var level = severity.ToString().Replace("CXDiagnostic_", string.Empty).ToUpper();

        // Only print warnings and errors, ignore notes/ignored stuff to keep the console clean
        if (severity >= ClangSharp.Interop.CXDiagnosticSeverity.CXDiagnostic_Warning)
        {
            Console.Error.WriteLine($"[{level}] {filename}: {diag.Spelling}");
        }

        if (
            severity
            is ClangSharp.Interop.CXDiagnosticSeverity.CXDiagnostic_Error
                or ClangSharp.Interop.CXDiagnosticSeverity.CXDiagnostic_Fatal
        )
        {
            hasErrors = true;
        }
    }

    return hasErrors;
}

static void PrintHelp()
{
    Console.WriteLine("Sharpie.CCompiler");
    Console.WriteLine("Usage: sharpie-cc <input1.c> [input2.c...] [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -O             Enable peephole optimizations and parameter promotion");
    Console.WriteLine("  -S             Emit readable .asm files instead of compiling to a .bin");
    Console.WriteLine("  -o <file>      Specify the output file name");
    Console.WriteLine("  -h, --help     Show this help screen");
}
