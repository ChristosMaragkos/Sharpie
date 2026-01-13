using System;
using System.Runtime.InteropServices.JavaScript;
using Raylib_cs;
using Sharpie.Runner.RaylibCs.Impl;

namespace Sharpie.Runner.Web;

public partial class Program
{
    private static RaylibVideoOutput video = null!;
    private static RaylibAudioOutput audio = null!;
    private static RaylibInputHandler input = null!;
    private static RaylibDebugOutpug logger = null!;
    private static SharpieConsole emulator = null!;
    private static byte[]? romBytes = null;

    public static void Main()
    {
        video = new RaylibVideoOutput();
        audio = new RaylibAudioOutput();
        input = new RaylibInputHandler();
        logger = new RaylibDebugOutpug(20);

        var biosBytes = BiosLoader.GetEmbeddedBiosBinary();
        
        emulator = new SharpieConsole(video, audio, input, logger);
        emulator.LoadBios(biosBytes);
    }

    [JSExport]
    public static void UpdateFrame()
    {
        emulator.Step();
        video.HandleFramebuffer(emulator.GetVideoBuffer());
        logger.LogAll();
    }

    [JSExport]
    public static bool IsInBootMode()
    {
        return emulator.IsInBootMode;
    }

    [JSExport]
    public static void LoadCartridgeFromBytes(byte[] data)
    {
        try
        {
            if (data != null && data.Length > 0)
            {
                romBytes = data;
                emulator.LoadCartridge(romBytes);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading cartridge: {e.Message}");
        }
    }
}
