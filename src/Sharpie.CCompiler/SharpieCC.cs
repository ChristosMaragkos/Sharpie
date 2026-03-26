using System.Text;
using Sharpie.CCompiler.NativeInterop;

namespace Sharpie.CCompiler;

public static class SharpieCC
{
    public static string Compile(IEnumerable<string> inputFiles, bool optimize)
    {
        LibClangResolver.Configure();

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

            if (HasErrors(tu, file))
            {
                throw new Exception($"Compilation failed due to frontend errors in {file}.");
            }

            var emitter = new SharpieEmitter(optimize);

            masterAssembly.AppendLine($"; ----------------------------------");
            masterAssembly.AppendLine($"; SOURCE: {Path.GetFileName(file)}");
            masterAssembly.AppendLine($"; ----------------------------------");
            masterAssembly.AppendLine(emitter.EmitTranslationUnit(tu.Cursor));
        }

        return masterAssembly.ToString();
    }

    private static bool HasErrors(ClangSharp.Interop.CXTranslationUnit tu, string filename)
    {
        var hasErrors = false;
        for (uint i = 0; i < tu.NumDiagnostics; i++)
        {
            using var diag = tu.GetDiagnostic(i);
            if (diag.Severity >= ClangSharp.Interop.CXDiagnosticSeverity.CXDiagnostic_Warning)
            {
                Console.Error.WriteLine(
                    $"[{diag.Severity.ToString().ToUpper()}] {filename}: {diag.Spelling}"
                );
            }

            if (diag.Severity >= ClangSharp.Interop.CXDiagnosticSeverity.CXDiagnostic_Error)
                hasErrors = true;
        }
        return hasErrors;
    }
}
