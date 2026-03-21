using Sharpie.CCompiler;
using Sharpie.CCompiler.NativeInterop;

LibClangResolver.Configure();

if (args.Contains("-h") || args.Contains("--help"))
{
    PrintHelp();
    return;
}

if (args.Length < 1)
{
    Console.Error.WriteLine("Missing input file.");
    PrintHelp();
    Environment.ExitCode = 1;
    return;
}

var inputPath = args[0];
var outputPath = args.Length >= 2 ? args[1] : Path.ChangeExtension(inputPath, ".asm");

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file not found: {inputPath}");
    Environment.ExitCode = 1;
    return;
}

try
{
    CompileFile(inputPath, outputPath);
    Console.WriteLine($"Wrote Sharpie assembly to {outputPath}");
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
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}

static void CompileFile(string inputPath, string outputPath)
{
    using var index = ClangSharp.Interop.CXIndex.Create();
    using var tu = ClangSharp.Interop.CXTranslationUnit.Parse(
        index,
        inputPath,
        new[] { "-std=gnu11", "-target", "msp430" }, // HACK: I'm passing some random 16-bit assembly to the frontend. I don't really like it.
        Array.Empty<ClangSharp.Interop.CXUnsavedFile>(),
        ClangSharp.Interop.CXTranslationUnit_Flags.CXTranslationUnit_None
    );

    var hasErrors = PrintDiagnostics(tu);
    if (hasErrors)
    {
        Environment.ExitCode = 1;
        return;
    }

    var emitter = new SharpieEmitter();
    var assembly = emitter.EmitTranslationUnit(tu.Cursor);
    File.WriteAllText(outputPath, assembly);
}

static bool PrintDiagnostics(ClangSharp.Interop.CXTranslationUnit tu)
{
    var hasErrors = false;

    for (uint i = 0; i < tu.NumDiagnostics; i++)
    {
        var diag = tu.GetDiagnostic(i);
        var severity = diag.Severity;
        var level = severity.ToString().Replace("CXDiagnostic_", string.Empty);
        Console.Error.WriteLine($"[{level}] {diag.Spelling}");

        if (
            severity
            is ClangSharp.Interop.CXDiagnosticSeverity.CXDiagnostic_Error
                or ClangSharp.Interop.CXDiagnosticSeverity.CXDiagnostic_Fatal
        )
            hasErrors = true;
    }

    return hasErrors;
}

static void PrintHelp()
{
    Console.WriteLine("Sharpie.CCompiler");
    Console.WriteLine("Usage: Sharpie.CCompiler <input.c> [output.asm]");
    Console.WriteLine(
        "Supported MVP: int main(void) with int locals, assignments, and arithmetic return expressions."
    );
}
