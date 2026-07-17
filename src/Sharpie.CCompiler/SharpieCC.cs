using System.Text;
using System.Text.RegularExpressions;
using ClangSharp.Interop;
using Sharpie.CCompiler.Emitter;

namespace Sharpie.CCompiler;

public static partial class SharpieCC
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
        if (allowLong)
            masterAssembly.Append(InjectedFunctions.GenerateAll());
        masterAssembly.AppendLine(".ENDREGION");

        using var index = CXIndex.Create();
        var clangArgs = new[] { "-std=gnu11", "-target", "msp430" };

        if (allowLong)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[WARNING] Compilation with 32-bit support enabled. Be mindful of the size of the emitted code.");
            Console.ResetColor();
        }

        // discover included headers and find their sibling .c files
        DiscoverSiblingCFiles(clangArgs, fileList);

        var emittedGlobals = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string>? reachableFunctions = null;

        if (optimize)
        {
            var globalCallGraph = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var globalAddressTaken = new HashSet<string>(StringComparer.Ordinal);
            var allFunctionNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var file in fileList)
            {
                using var tu = CXTranslationUnit.Parse(
                    index,
                    file,
                    clangArgs,
                    [],
                    CXTranslationUnit_Flags.CXTranslationUnit_None
                );

                if (HasErrors(tu, file))
                {
                    throw new Exception($"Compilation failed due to frontend errors in {file}.");
                }

                var (fileCallGraph, fileAddressTaken) = SharpieEmitter.CollectCallGraph(tu.Cursor);

                globalAddressTaken.UnionWith(fileAddressTaken);

                foreach (var (caller, callees) in fileCallGraph)
                {
                    if (globalCallGraph.TryGetValue(caller, out var existingCallees))
                        existingCallees.UnionWith(callees);
                    else
                        globalCallGraph[caller] = new HashSet<string>(callees, StringComparer.Ordinal);
                }

                var functions = GetChildren(tu.Cursor)
                    .Where(c => c.Kind == CXCursorKind.CXCursor_FunctionDecl)
                    .ToList();

                foreach (var func in functions)
                {
                    if (GetChildren(func).Any(c => c.Kind == CXCursorKind.CXCursor_CompoundStmt))
                        allFunctionNames.Add(func.Spelling.ToString());
                }
            }

            if (allFunctionNames.Contains("main"))
            {
                reachableFunctions = SharpieEmitter.ComputeReachability(
                    globalCallGraph, globalAddressTaken, allFunctionNames
                );

            }
        }

        foreach (var file in fileList)
        {
            using var tu = CXTranslationUnit.Parse(
                index,
                file,
                clangArgs,
                [],
                CXTranslationUnit_Flags.CXTranslationUnit_None
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

    private static void DiscoverSiblingCFiles(string[] clangArgs, List<string> fileList)
    {
        // Extract include directories from clang args
        var includeDirs = new List<string>();
        for (int i = 0; i < clangArgs.Length - 1; i++)
        {
            if (clangArgs[i] == "-I" && !string.IsNullOrEmpty(clangArgs[i + 1]))
                includeDirs.Add(clangArgs[i + 1]);
        }

        var knownFiles = fileList
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            var count = fileList.Count;

            foreach (var file in fileList.ToList())
            {
                var content = File.ReadAllText(file);
                var includeRegex = MyRegex();

                foreach (Match match in includeRegex.Matches(content))
                {
                    var includePath = match.Groups[1].Value;
                    string? resolvedPath = null;

                    // For "" includes, check relative to the including file's directory first
                    if (match.Value.Contains('"'))
                    {
                        var fileDir = Path.GetDirectoryName(Path.GetFullPath(file))!;
                        var relative = Path.GetFullPath(Path.Combine(fileDir, includePath));
                        if (File.Exists(relative))
                            resolvedPath = relative;
                    }

                    // Check include directories
                    if (resolvedPath == null)
                    {
                        foreach (var dir in includeDirs)
                        {
                            var candidate = Path.GetFullPath(Path.Combine(dir, includePath));
                            if (File.Exists(candidate))
                            {
                                resolvedPath = candidate;
                                break;
                            }
                        }
                    }

                    if (resolvedPath?.EndsWith(".h", StringComparison.OrdinalIgnoreCase) != true)
                        continue;

                    var siblingC = Path.ChangeExtension(resolvedPath, ".c");
                    if (!File.Exists(siblingC))
                        continue;

                    var fullPath = Path.GetFullPath(siblingC);
                    if (knownFiles.Add(fullPath))
                        fileList.Add(fullPath);
                }
            }

            if (fileList.Count == count)
                break;
        }
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

    private static bool HasErrors(CXTranslationUnit tu, string filename)
    {
        var hasErrors = false;
        for (uint i = 0; i < tu.NumDiagnostics; i++)
        {
            using var diag = tu.GetDiagnostic(i);
            if (diag.Severity >= CXDiagnosticSeverity.CXDiagnostic_Warning)
            {
                Console.Error.WriteLine(
                    $"[{diag.Severity.ToString().ToUpper()}] {filename}: {diag.Spelling}"
                );
            }

            if (diag.Severity >= CXDiagnosticSeverity.CXDiagnostic_Error)
                hasErrors = true;
        }
        return hasErrors;
    }

    [GeneratedRegex(@"#include\s+[""<]([^"">]+)[>"">]"
    )]
    private static partial Regex MyRegex();
}
