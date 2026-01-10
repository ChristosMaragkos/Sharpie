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
        0xFF,
        0xFF,
    };

    public static void Main(string[] args)
    {
        var inputFile = "";
        var outputFile = "";
        var isFirmware = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-i":
                case "--input":
                    if (i + 1 < args.Length)
                        inputFile = args[++i];
                    break;
                case "-o":
                case "--output":
                    if (i + 1 < args.Length)
                        outputFile = args[++i];
                    break;
                case "-f":
                case "--firmware":
                    isFirmware = true;
                    break;
                case "-h":
                case "--help":
                    PrintHelp();
                    return;
            }
        }

        if (string.IsNullOrWhiteSpace(inputFile))
        {
            Console.WriteLine("Error: No input file specified. use -i <path-to-asm-file>");
            return;
        }

        if (!inputFile.EndsWith(".asm"))
        {
            Console.WriteLine(
                "Error: Input is not a file containing assembly code or does not have the '.asm' format."
            );
            return;
        }

        if (string.IsNullOrWhiteSpace(outputFile))
        {
            outputFile = Path.ChangeExtension(inputFile, isFirmware ? ".bin" : ".shr");
        }

        try
        {
            var asm = new Assembler();
            asm.LoadFile(inputFile);

            var exporter = new Exporter(GameTitle, AuthorName, outputFile, InitialPalette);
            exporter.ExportRom(asm.Rom, isFirmware);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                isFirmware ? "Firmware Build Successful!" : "Cartridge export successful!"
            );
            Console.ResetColor();
            Environment.Exit(0);
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[FATAL ERROR] {e.GetType().Name}");
            Console.WriteLine(e.Message);
            Console.ResetColor();
            Environment.Exit(1);
        }

        Environment.Exit(0);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("----------------Sharpie SDK Cheat-Sheet----------------");
        Console.WriteLine(
            "-i | --input     Specify the input file to assemble. Must be a .asm file."
        );
        Console.WriteLine();
        Console.WriteLine(
            "-o | --output    Specify the name of the output file to export to, without any extensions."
        );
        Console.WriteLine();
        Console.WriteLine(
            "-f | --firmware  Export ROM as firmware (skip header and write to .bin file)."
        );
        Console.WriteLine();
        Console.WriteLine("-h | --help      Display this help message.");
        Console.WriteLine("-------------------------------------------------------");
    }
}
