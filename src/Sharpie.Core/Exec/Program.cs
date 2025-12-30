using Sharpie.Core;

Console.WriteLine("Booting Sharpie...");
var mobo = new Motherboard();

// TODO: boot sequence ("SHARPIE" slides into screen)

if (args.Length > 0 && File.Exists(args[0]))
{
    try
    {
        var cart = Cartridge.Load(args[0]);
        Console.WriteLine($"Booting into cartridge: {cart.Title}");
        mobo.BootCartridge(cart);
    }
    catch (Exception e)
    {
        Console.WriteLine($"Could not load cartridge: {e.Message}");
        return;
    }
}
else
{
    Console.WriteLine("No cartridge found. Booting system shell...");
    // TODO: load "Please Insert Cartridge" scene from bootloader/firmware
}
