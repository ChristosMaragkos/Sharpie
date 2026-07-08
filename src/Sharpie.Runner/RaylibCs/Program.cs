using System.Runtime.InteropServices;
using Raylib_cs;
using Sharpie.Core;
using Sharpie.Runner.RaylibCs.Impl;

var video = new RaylibVideoOutput();
var audio = new RaylibAudioOutput();
var input = new RaylibInputHandler();
var logger = new RaylibDebugOutpug(20);
var saveHandler = new RaylibSaveHandler();

var biosBytes = BiosLoader.GetEmbeddedBiosBinary();
byte[]? romBytes = null;

if (args.Length != 0)
{
    try
    {
        if (!args[0].EndsWith(".shr"))
            throw new FormatException("Sharpie ROM files must end with the .shr extension.");
        romBytes = File.ReadAllBytes(args[0]);
        saveHandler.SavePath = Path.ChangeExtension(args[0], ".sav");
    }
    catch (Exception e)
    {
        Console.WriteLine($"Error loading ROM: {e.Message}");
    }
}

var emulator = new SharpieConsole(video, audio, input, logger, saveHandler);
emulator.LoadBios(biosBytes);

#if Linux
var icon = Raylib.LoadImage(Path.Combine(Directory.GetCurrentDirectory(), "icon.png"));
Raylib.SetWindowIcon(icon);
Raylib.UnloadImage(icon);
#elif Windows
unsafe
{
    var iconBytes = BiosLoader.GetEmbeddedIcon();
    fixed (byte* pData = iconBytes)
    {
        byte[] ext = { (byte)'.', (byte)'p', (byte)'n', (byte)'g' };
        fixed (byte* pExt = ext)
        {
            var icon = Raylib.LoadImageFromMemory((sbyte*)pExt, pData, iconBytes.Length);

            if (Raylib.IsImageValid(icon))
            {
                Raylib.SetWindowIcon(icon);
                Raylib.UnloadImage(icon);
            }
        }
    }
}
#endif

TryLoadCart();

while (!video.ShouldCloseWindow())
{
    // Check for mouse click when in boot mode (Please Insert Cartridge screen)
    if (emulator.IsInBootMode && Raylib.IsMouseButtonPressed(MouseButton.Left))
    {
        try
        {
            var filePath = FileDialog.OpenFileDialog();
            if (filePath?.EndsWith(".shr") == true)
            {
                romBytes = File.ReadAllBytes(filePath);
                saveHandler.SavePath = Path.ChangeExtension(filePath, ".sav");
                TryLoadCart();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading ROM: {e.Message}");
        }
    }

    if (Raylib.IsFileDropped() && emulator.IsInBootMode)
    {
        try
        {
            unsafe
            {
                var droppedFiles = Raylib.LoadDroppedFiles();
                var cartridgeFile = PointerToString(droppedFiles.Paths[0]);
                saveHandler.SavePath = Path.ChangeExtension(cartridgeFile, ".sav");
                if (!cartridgeFile.EndsWith(".shr"))
                    Console.WriteLine("Sharpie ROM files must end with the .shr extension.");

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
    if (!emulator.IsForcedUpdate)
    {
        video.HandleFramebuffer(emulator.GetVideoBuffer());
    }
    logger.LogAll();
}

video.Cleanup();
audio.Cleanup();

return emulator.ExitCode;

void TryLoadCart()
{
    if (romBytes != null)
    {
        Console.WriteLine($"Set up save callback with path: {saveHandler.SavePath}");
        emulator.LoadCartridge(romBytes);
    }
}

unsafe string PointerToString(byte* ptr) =>
    Marshal.PtrToStringUTF8((IntPtr)ptr)
    ?? throw new Exception("I don't even know what exception to throw here");
