using System.Reflection;
using Sharpie.Core;
using Sharpie.Runner.Headless.Impl;

namespace Sharpie.Runner.Headless;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Headless runner requires a path to the cartridge as the first argument. Stop.");
            return 1;
        }

        HeadlessVideoOutput video = new();
        HeadlessAudioOutput audio = new();
        HeadlessInputHandler input = new();
        HeadlessDebugOutput debug = new(100);
        HeadlessSaveHandler saveHandler = new();
        SharpieConsole emulator = new(video, audio, input, debug, saveHandler);
        emulator.LoadBios(GetEmbeddedBiosBinary());
        emulator.LoadCartridge(File.ReadAllBytes(args[0]));

        while (!video.ShouldCloseWindow(emulator))
        {
            emulator.Step();
            debug.LogAll();
        }

        return emulator.ExitCode;
    }

    private static byte[] GetEmbeddedBiosBinary()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream =
            assembly.GetManifestResourceStream("bios.bin")
            ?? throw new ApplicationException("BIOS binary not found.");
        byte[] ba = new byte[stream.Length];
        stream.ReadExactly(ba, 0, ba.Length);
        return ba;
    }
}
