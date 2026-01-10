using System.Runtime.InteropServices;
using Raylib_cs;
using Sharpie.Runner.RaylibCs.Impl;

var video = new RaylibVideoOutput();
var audio = new RaylibAudioOutput();
var input = new RaylibInputHandler();
var logger = new RaylibDebugOutpug(20);

var biosBytes = BiosLoader.GetEmbeddedBiosBinary();
byte[]? romBytes = null;

if (args.Length != 0)
{
    try
    {
        if (!args[0].EndsWith(".shr"))
            throw new FormatException("Sharpie ROM files must end with the .shr extension.");
        romBytes = File.ReadAllBytes(args[0]);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error loading ROM: {e.Message}");
    }
}

var emulator = new SharpieConsole(video, audio, input, logger);
emulator.LoadBios(biosBytes);

TryLoadCart();

while (!video.ShouldCloseWindow())
{
    if (Raylib.IsFileDropped() && emulator.IsInBootMode)
    {
        try
        {
            unsafe
            {
                var droppedFiles = Raylib.LoadDroppedFiles();
                var cartridgeFile = PointerToString(droppedFiles.Paths[0]);
                if (!cartridgeFile.EndsWith(".shr"))
                    Console.WriteLine($"Sharpie ROM files must end with the .shr extension.");

                Raylib.UnloadDroppedFiles(droppedFiles);
                romBytes = File.ReadAllBytes(cartridgeFile);
                TryLoadCart();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading ROM: {e.Message}");
        }
    }
    emulator.Step();
    video.HandleFramebuffer(emulator.GetVideoBuffer());
    logger.LogAll();
}

video.Cleanup();
audio.Cleanup();

return;

void TryLoadCart()
{
    if (romBytes != null)
    {
        emulator.LoadCartridge(romBytes);
    }
}

unsafe string PointerToString(byte* ptr) =>
    Marshal.PtrToStringUTF8((IntPtr)ptr)
    ?? throw new Exception($"I don't even know what exception to throw here");
