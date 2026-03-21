using System.Reflection;
using System.Runtime.InteropServices;
using ClangSharp.Interop;

namespace Sharpie.CCompiler.NativeInterop;

internal static class LibClangResolver
{
    private static bool _configured;
    private static nint _preloadedHandle;

    public static void Configure()
    {
        if (_configured)
            return;

        _configured = true;

        var candidates = GetCandidates().Distinct(StringComparer.Ordinal).ToArray();
        PreloadFirstAvailableCandidate(candidates);
        clang.ResolveLibrary += ResolveLibrary;
    }

    private static void PreloadFirstAvailableCandidate(IEnumerable<string> candidates)
    {
        if (_preloadedHandle != 0)
            return;

        foreach (var candidate in candidates)
        {
            try
            {
                if (NativeLibrary.TryLoad(candidate, out _preloadedHandle))
                    return;
            }
            catch
            {
                // Try the next candidate.
            }
        }
    }

    private static IntPtr ResolveLibrary(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath
    )
    {
        if (!IsLibClangName(libraryName))
            return IntPtr.Zero;

        if (_preloadedHandle != 0)
            return _preloadedHandle;

        foreach (var candidate in GetCandidates().Distinct(StringComparer.Ordinal))
        {
            try
            {
                if (NativeLibrary.TryLoad(candidate, assembly, searchPath, out var handle))
                    return handle;
            }
            catch
            {
                // Try the next candidate.
            }

            try
            {
                if (NativeLibrary.TryLoad(candidate, out var handle))
                    return handle;
            }
            catch
            {
                // Try the next candidate.
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

        return false;
    }

    private static IEnumerable<string> GetCandidates()
    {
        var appDir = AppContext.BaseDirectory;
        var explicitPath = Environment.GetEnvironmentVariable("SHARPIE_LIBCLANG_PATH");
        var explicitName = Environment.GetEnvironmentVariable("SHARPIE_LIBCLANG_NAME");

        if (!string.IsNullOrWhiteSpace(explicitPath))
            yield return explicitPath;

        if (!string.IsNullOrWhiteSpace(explicitName))
            yield return explicitName;

        foreach (var localCandidate in GetLocalRuntimeCandidates(appDir))
            yield return localCandidate;

        foreach (var defaultName in GetDefaultLibraryNames())
            yield return defaultName;
    }

    private static IEnumerable<string> GetLocalRuntimeCandidates(string appDir)
    {
        foreach (var fileName in GetDefaultLibraryNames())
        {
            yield return Path.Combine(appDir, fileName);
            yield return Path.Combine(appDir, "runtimes", GetRuntimeFolder(), "native", fileName);
        }
    }

    private static string GetRuntimeFolder()
    {
        if (OperatingSystem.IsWindows())
            return Environment.Is64BitProcess ? "win-x64" : "win-x86";

        if (OperatingSystem.IsMacOS())
            return Environment.Is64BitProcess ? "osx-x64" : "osx";

        return Environment.Is64BitProcess ? "linux-x64" : "linux-x86";
    }

    private static IEnumerable<string> GetDefaultLibraryNames()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return "libclang.dll";
            yield return "clang.dll";
            yield break;
        }

        if (OperatingSystem.IsMacOS())
        {
            yield return "libclang.dylib";
            yield return "libclang";
            yield break;
        }

        yield return "libclang.so";
        yield return "libclang.so.1";
        yield return "libclang";
    }
}
