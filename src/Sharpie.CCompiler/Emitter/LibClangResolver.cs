using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using ClangSharp.Interop;

namespace Sharpie.CCompiler.Emitter;

internal static class LibClangResolver
{
    private static bool _configured;
    private static readonly ConcurrentDictionary<string, nint> _loadedHandles = new(StringComparer.OrdinalIgnoreCase);

    public static void Configure()
    {
        if (_configured)
            return;

        _configured = true;
        clang.ResolveLibrary += ResolveLibrary;
    }

    private static IntPtr ResolveLibrary(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath
    )
    {
        if (!IsLibClangName(libraryName))
            return IntPtr.Zero;

        if (_loadedHandles.TryGetValue(libraryName, out var cached))
            return cached;

        bool isClangSharp = libraryName.StartsWith("libClangSharp", StringComparison.OrdinalIgnoreCase);
        var candidates = isClangSharp
            ? GetClangSharpCandidates()
            : GetLibclangCandidates();

        foreach (var candidate in candidates.Distinct(StringComparer.Ordinal))
        {
            try
            {
                if (NativeLibrary.TryLoad(candidate, assembly, searchPath, out var handle))
                {
                    _loadedHandles[libraryName] = handle;
                    return handle;
                }
            }
            catch
            {
            }

            try
            {
                if (NativeLibrary.TryLoad(candidate, out var handle))
                {
                    _loadedHandles[libraryName] = handle;
                    return handle;
                }
            }
            catch
            {
            }
        }

        return IntPtr.Zero;
    }

    private static bool IsLibClangName(string libraryName)
    {
        if (string.Equals(libraryName, "libclang", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(libraryName, "libclang.so", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(libraryName, "libclang.dylib", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(libraryName, "libclang.dll", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(libraryName, "libClangSharp", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(libraryName, "libClangSharp.so", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(libraryName, "libClangSharp.dylib", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(libraryName, "libClangSharp.dll", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static IEnumerable<string> GetLibclangCandidates()
    {
        var appDir = AppContext.BaseDirectory;
        var explicitPath = Environment.GetEnvironmentVariable("SHARPIE_LIBCLANG_PATH");
        var explicitName = Environment.GetEnvironmentVariable("SHARPIE_LIBCLANG_NAME");

        if (!string.IsNullOrWhiteSpace(explicitPath))
            yield return explicitPath;

        if (!string.IsNullOrWhiteSpace(explicitName))
            yield return explicitName;

        // App-local paths
        yield return Path.Combine(appDir, "libclang.so");
        yield return Path.Combine(appDir, "libclang.so.1");
        yield return Path.Combine(appDir, "libclang");
        yield return Path.Combine(appDir, "runtimes", GetRuntimeFolder(), "native", "libclang.so");
        yield return Path.Combine(appDir, "runtimes", GetRuntimeFolder(), "native", "libclang.so.1");

        // System defaults
        yield return "libclang.so";
        yield return "libclang.so.1";
        yield return "libclang";
    }

    private static IEnumerable<string> GetClangSharpCandidates()
    {
        var appDir = AppContext.BaseDirectory;

        // App-local paths
        yield return Path.Combine(appDir, "libClangSharp.so");
        yield return Path.Combine(appDir, "libClangSharp");
        yield return Path.Combine(appDir, "runtimes", GetRuntimeFolder(), "native", "libClangSharp.so");

        // System defaults
        yield return "libClangSharp.so";
        yield return "libClangSharp";
    }

    private static string GetRuntimeFolder()
    {
        if (OperatingSystem.IsWindows())
            return Environment.Is64BitProcess ? "win-x64" : "win-x86";

        if (OperatingSystem.IsMacOS())
            return Environment.Is64BitProcess ? "osx-x64" : "osx";

        return Environment.Is64BitProcess ? "linux-x64" : "linux-x86";
    }
}
