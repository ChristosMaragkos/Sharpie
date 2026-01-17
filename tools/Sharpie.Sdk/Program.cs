using System.Text.Json;
using Sharpie.Sdk.Asm;
using Sharpie.Sdk.Meta;
using Sharpie.Sdk.Serialization;

namespace Sharpie.Sdk;

internal class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("-h") || args.Contains("--help"))
        {
            PrintHelp();
            Environment.Exit(0);
        }

        var guiMode =
            args.Length == 0
            || (
                args.ContainsAny("--sprite-editor", "-se")
                || args.ContainsAny("--music-editor", "-me")
            );

        if (guiMode)
        {
            if (args.Length == 0)
            {
                MainGui();
                return;
            }

            if (args.All(str => str == "--sprite-editor" || str == "-se"))
            {
                SpriteEditorGui();
                return;
            }
            else if (args.All(str => str == "--music-editor" || str == "-me"))
            {
                MusicEditorGui();
                return;
            }
            PrintHelp();
            return;
        }

        CliMode(args);
    }

    private static void MainGui()
    {
        Gui.MainScreen.RunMainGui();
    }

    private static void SpriteEditorGui()
    {
        throw new NotImplementedException();
    }

    private static void MusicEditorGui()
    {
        Console.WriteLine("Music Editor coming soon™!");
    }

    private static void CliMode(string[] args)
    {
        var manifestPath = args.FirstOrDefault(a => a.EndsWith(".json"));
        ProjectManifest manifest;
        var manifestDumpPath = "";

        if (!string.IsNullOrWhiteSpace(manifestPath) && File.Exists(manifestPath))
        {
            manifest = JsonSerializer.Deserialize<ProjectManifest>(
                File.ReadAllText(manifestPath),
                SharpieJsonContext.Default.ProjectManifest
            )!;
        }
        else
        {
            var input = "";
            var output = "";
            var firmware = false;
            var author = "";
            var title = "";

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-i":
                    case "--input":
                        if (i + 1 < args.Length)
                            input = args[++i];
                        break;

                    case "-o":
                    case "--output":
                        if (i + 1 < args.Length)
                            output = args[++i];
                        break;

                    case "-f":
                    case "--firmware":
                        firmware = true;
                        break;

                    case "-a":
                    case "--author":
                        if (i + 1 < args.Length)
                            author = args[++i];
                        break;

                    case "-t":
                    case "--title":
                        if (i + 1 < args.Length)
                            title = args[++i];
                        break;

                    case "-c":
                    case "--create-manifest":
                        if (i + 1 < args.Length)
                            manifestDumpPath = args[++i];
                        break;
                }
            }
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(input))
                missing.Add("input (-i / --input)");
            if (string.IsNullOrWhiteSpace(title) && !firmware)
                missing.Add("title (-t / --title)");
            if (string.IsNullOrWhiteSpace(author) && !firmware)
                missing.Add("author (-a / --author)");

            if (missing.Count > 0)
            {
                Console.WriteLine(
                    "[ERROR] Missing required arguments: " + string.Join(", ", missing)
                );
                return;
            }

            manifest = new ProjectManifest(
                title,
                author,
                input,
                output ?? Path.ChangeExtension(input, firmware ? ".bin" : ".shr"),
                firmware,
                ProjectManifest.DefaultPalette(),
                Constants.BiosVersion
            );
        }

        var validation = manifest.Validate(Constants.BiosVersion);
        if (!validation.IsValid)
        {
            Console.WriteLine("B");
            foreach (var e in validation.Errors)
                Console.WriteLine($"[ERROR] {e}");
            return;
        }

        try
        {
            AssembleRom(
                manifest.InputPath,
                manifest.Title,
                manifest.Author,
                manifest.ResolveOutputPath(),
                manifest.Palette,
                manifest.IsFirmware
            );
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                manifest.IsFirmware ? "Firmware build successful!" : "Cartridge export successful!"
            );
            Console.ResetColor();
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ASSEMBLER ERROR] {e.Message}");
            Console.ResetColor();
        }

        if (!string.IsNullOrWhiteSpace(manifestDumpPath))
        {
            try
            {
                Console.WriteLine("Creating manifest file...");
                var json = JsonSerializer.Serialize(
                    manifest,
                    SharpieJsonContext.Default.ProjectManifest
                );
                if (string.IsNullOrWhiteSpace(json))
                    throw new JsonException("Could not serialize project manifest.");

                if (!manifestDumpPath.EndsWith(".json"))
                    throw new FormatException("Output path is not a JSON file.");

                File.WriteAllText(manifestDumpPath, json);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Project manifest saved sucessfully!");
                Console.ResetColor();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error saving manifest file: {e.Message}");
                Console.ResetColor();
            }
        }
    }

    public static void AssembleRom(
        string inputFilePath,
        string romTitle,
        string romAuthor,
        string outputFilePath,
        int[] defaultPalette,
        bool isFirmware = false
    )
    {
        var asm = new Assembler();
        asm.LoadFile(inputFilePath);

        var list = defaultPalette.ToList();

        while (defaultPalette.Length < 16)
        {
            list.Add(0xFF);
        }
        while (defaultPalette.Length > 16)
        {
            list.RemoveAt(list.Count - 1);
        }
        defaultPalette = list.ToArray();

        var exporter = new Exporter(romTitle, romAuthor, outputFilePath, defaultPalette);
        exporter.ExportRom(asm.Rom, isFirmware);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("----------------Sharpie SDK----------------------------");
        Console.WriteLine(
            "-i | --input     Specify the input file to assemble. Must be a .asm file."
        );
        Console.WriteLine();
        Console.WriteLine("-o | --output    Specify the name of the output file to export to");
        Console.WriteLine();
        Console.WriteLine("-f | --firmware  Export ROM as firmware (skip header)");
        Console.WriteLine();
        Console.WriteLine("-h | --help      Display this help message.");
        Console.WriteLine();
        Console.WriteLine("Leave blank for GUI mode.");
        Console.WriteLine();
        Console.WriteLine("--sprite-editor | -se    Open Sprite Editor");
        Console.WriteLine("--music-editor | -me     Open Music Editor");
        Console.WriteLine("-------------------------------------------------------");
    }
}
