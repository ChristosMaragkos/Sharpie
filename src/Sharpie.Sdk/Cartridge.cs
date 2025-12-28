using Sharpie.Sdk.Asm;

namespace Sharpie.Sdk;

internal class Cartridge
{
    // Change these to whatever you'd like!
    public const string GameTitle = "Test ROM";
    public const string AuthorName = "ChrisMaragkos";
    public const string GameRomName = "test"; // No file extension needed.
    public static readonly byte[] InitialPalette = new byte[] // Change any of these to any number from 0-31 to override a color.
    { // Note that color 0 is always transparent.
        0xFF,
        0x0A,
        0x14,
        0xFF,
        0xFF,
        0xFF,
        0xFF,
        0xFF,
        0xFF,
        0xFF,
        0xFF,
        0xFF,
        0xFF,
        0xFF,
        0xFF,
        0xFF,
    };

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: sharpie.exe <path-to-asm-file>");
            return;
        }

        var fp = args[0];

        var outputFile = string.IsNullOrWhiteSpace(GameRomName)
            ? Path.ChangeExtension(fp, ".shr")
            : Path.ChangeExtension(GameRomName, ".shr");

        try
        {
            var asm = new Assembler();
            asm.LoadFile(fp);

            var exporter = new Exporter(GameTitle, AuthorName, outputFile, InitialPalette);
            exporter.ExportRom(asm.Rom);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Build Successful!");
            Console.ResetColor();
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[FATAL ERROR] {e.GetType().Name}");
            Console.WriteLine(e.Message);
            Console.ResetColor();

            Environment.Exit(1);
        }
    }
}
