using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Sharpie.Tests;

// NOTE: If the compiler changes, run rm -f fixture_cache.json to force a full re-test.
public class CCompilerTests
{
    private static readonly string OutputDir =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    private static readonly string FixturesDir = Path.Combine(OutputDir, "fixtures");
    private static readonly string CachePath = Path.Combine(OutputDir, "fixture_cache.json");
    private static readonly string CliPath = Path.Combine(OutputDir, "sharpie.dll");
    private static readonly string HeadlessPath = Path.Combine(OutputDir, "Sharpie.Runner.Headless.dll");
    private static readonly string[] Phases = ["compile", "run-o0", "run-o"];

    private static string HashFile(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static Dictionary<string, Dictionary<string, string>> LoadCache()
    {
        if (!File.Exists(CachePath)) return [];
        return JsonSerializer.Deserialize<
            Dictionary<string, Dictionary<string, string>>
        >(File.ReadAllText(CachePath)) ?? [];
    }

    private static void WriteCache(Dictionary<string, Dictionary<string, string>> cache)
    {
        var dir = Path.GetDirectoryName(CachePath)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(CachePath, JsonSerializer.Serialize(cache));
    }

    private static int RunProcess(string dllPath, string args)
    {
        var psi = new ProcessStartInfo("dotnet", $"{dllPath} {args}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        // Pass through env vars needed for libclang resolution
        psi.EnvironmentVariables["SHARPIE_LIBCLANG_PATH"] =
            Environment.GetEnvironmentVariable("SHARPIE_LIBCLANG_PATH") ?? "";
        var ldLib = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";
        if (ldLib != "")
            psi.EnvironmentVariables["LD_LIBRARY_PATH"] = ldLib;

        using var proc = Process.Start(psi);
        proc!.WaitForExit(30000);
        return proc.ExitCode;
    }

    public static TheoryData<string> GetFixtures()
    {
        var files = Directory.GetFiles(FixturesDir, "*.c", SearchOption.AllDirectories);
        var cache = LoadCache();

        files = [.. files.Where(f =>
        {
            var hash = HashFile(f);
            return !cache.TryGetValue(f, out var entry)
                || entry.GetValueOrDefault("hash") != hash
                || !Phases.All(p => entry.ContainsKey(p));
        })];

        return new TheoryData<string>(files);
    }

    private static int RunCompiler(string fixturePath)
    {
        var outputPath = Path.ChangeExtension(fixturePath, ".shr");
        return RunProcess(CliPath, $"\"{fixturePath}\" -o \"{outputPath}\" --allow-long");
    }

    private static int CompileAndRun(string fixturePath, bool optimize)
    {
        var outputPath = Path.ChangeExtension(fixturePath, ".shr");

        var optFlag = optimize ? "-O " : "";
        var exitCode = RunProcess(CliPath, $"\"{fixturePath}\" -o \"{outputPath}\" {optFlag}--allow-long");
        if (exitCode != 0) return exitCode;

        return RunProcess(HeadlessPath, $"\"{outputPath}\"");
    }

    private static void MarkCached(string fixturePath, string phase)
    {
        var cache = LoadCache();
        if (!cache.TryGetValue(fixturePath, out var entry))
            cache[fixturePath] = entry = [];

        entry["hash"] = HashFile(fixturePath);
        entry[phase] = "ok";
        WriteCache(cache);
    }

    [Theory]
    [MemberData(nameof(GetFixtures))]
    public void Compiles(string fixturePath)
    {
        Assert.True(RunCompiler(fixturePath) == 0, $"{fixturePath} failed to compile altogether.");
        MarkCached(fixturePath, "compile");
    }

    [Theory]
    [MemberData(nameof(GetFixtures))]
    public void RunsCorrectly_Unoptimized(string fixturePath)
    {
        Assert.True(CompileAndRun(fixturePath, optimize: false) == 0, $"{fixturePath} failed to run optimized.");
        MarkCached(fixturePath, "run-o0");
    }

    [Theory]
    [MemberData(nameof(GetFixtures))]
    public void RunsCorrectly_Optimized(string fixturePath)
    {
        Assert.True(CompileAndRun(fixturePath, optimize: true) == 0, $"{fixturePath} failed to run optimized.");
        MarkCached(fixturePath, "run-o");
    }
}
