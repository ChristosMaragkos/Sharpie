using System.Text;
using ClangSharp.Interop;
using Sharpie.CCompiler.Emitter;

namespace Sharpie.CCompiler;

public static class SharpieCC
{
    public static string Compile(IEnumerable<string> inputFiles, bool optimize, bool allowLong = false)
    {
        LibClangResolver.Configure();

        var fileList = inputFiles.ToList();

        var masterAssembly = new StringBuilder();
        masterAssembly.AppendLine("; ------------------------");
        masterAssembly.AppendLine("; Sharpie C cartridge");
        masterAssembly.AppendLine("; ------------------------");
        masterAssembly.AppendLine(".REGION FIXED");
        masterAssembly.AppendLine("    JMP Main");
        masterAssembly.AppendLine(".ENDREGION");

        using var index = ClangSharp.Interop.CXIndex.Create();
        var clangArgs = new[] { "-std=gnu11", "-target", "msp430" };

        if (allowLong)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[WARNING] Compilation with 32-bit support enabled. Be mindful of the size of the emitted code.");
            Console.ResetColor();
        }

        var emittedGlobals = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string>? reachableFunctions = null;

        if (optimize)
        {
            var globalCallGraph = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var globalAddressTaken = new HashSet<string>(StringComparer.Ordinal);
            var allFunctionNames = new HashSet<string>(StringComparer.Ordinal);

            // Pass 1: collect call graph from all input files
            foreach (var file in fileList)
            {
                using var tu = ClangSharp.Interop.CXTranslationUnit.Parse(
                    index,
                    file,
                    clangArgs,
                    [],
                    ClangSharp.Interop.CXTranslationUnit_Flags.CXTranslationUnit_None
                );

                if (HasErrors(tu, file))
                {
                    throw new Exception($"Compilation failed due to frontend errors in {file}.");
                }

                var (fileCallGraph, fileAddressTaken) = SharpieEmitter.CollectCallGraph(tu.Cursor);

                // Add address-taken functions
                globalAddressTaken.UnionWith(fileAddressTaken);

                // Add call graph edges
                foreach (var (caller, callees) in fileCallGraph)
                {
                    if (globalCallGraph.TryGetValue(caller, out var existingCallees))
                        existingCallees.UnionWith(callees);
                    else
                        globalCallGraph[caller] = new HashSet<string>(callees, StringComparer.Ordinal);
                }

                // Track all function names from this TU (with bodies)
                var functions = GetChildren(tu.Cursor)
                    .Where(c => c.Kind == CXCursorKind.CXCursor_FunctionDecl)
                    .ToList();

                foreach (var func in functions)
                {
                    if (GetChildren(func).Any(c => c.Kind == CXCursorKind.CXCursor_CompoundStmt))
                        allFunctionNames.Add(func.Spelling.ToString());
                }
            }

            // Only apply DFE if 'main' exists in the program
            if (allFunctionNames.Contains("main"))
            {
                reachableFunctions = SharpieEmitter.ComputeReachability(
                    globalCallGraph, globalAddressTaken, allFunctionNames
                );
            }
        }

        // Pass 2 (or only pass if !optimize): emit
        foreach (var file in fileList)
        {
            using var tu = ClangSharp.Interop.CXTranslationUnit.Parse(
                index,
                file,
                clangArgs,
                [],
                ClangSharp.Interop.CXTranslationUnit_Flags.CXTranslationUnit_None
            );

            if (HasErrors(tu, file))
            {
                throw new Exception($"Compilation failed due to frontend errors in {file}.");
            }

            var emitter = new SharpieEmitter(optimize, allowLong, emittedGlobals, reachableFunctions);

            masterAssembly.AppendLine("; ----------------------------------");
            masterAssembly.AppendLine($"; SOURCE: {Path.GetFileName(file)}");
            masterAssembly.AppendLine("; ----------------------------------");
            masterAssembly.AppendLine("");
            masterAssembly.AppendLine(emitter.EmitTranslationUnit(tu.Cursor));
        }

        return masterAssembly.ToString();
    }

    private static List<CXCursor> GetChildren(CXCursor cursor)
    {
        var children = new List<CXCursor>();

        unsafe
        {
            cursor.VisitChildren(
                (child, _, _) =>
                {
                    children.Add(child);
                    return CXChildVisitResult.CXChildVisit_Continue;
                },
                new CXClientData(IntPtr.Zero)
            );
        }

        return children;
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
