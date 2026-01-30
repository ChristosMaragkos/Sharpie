using System.Text.Json;
using Sharpie.Sdk.Asm;
using Sharpie.Sdk.Gui;
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

        var guiMode = args.Length == 0;

        if (guiMode)
        {
            if (args.Length == 0)
            {
                MainGui();
                return;
            }
            return;
        }

        CliMode(args);
    }

    private static void MainGui()
    {
        Gui.MainScreen.RunMainGui();
    }

    private static void CliMode(string[] args)
    {
        string? manifestPath = null;
        var pngFiles = new List<string>();
        string manifestDumpPath = "";

        string input = "";
        string output = "";
        bool firmware = false;
        string author = "";
        string title = "";

        // --- Parse args ---
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-m":
                case "--manifest":
                    if (i + 1 < args.Length)
                        manifestPath = args[++i];
                    break;

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

                case "-p":
                case "--parse-png":
                    if (i + 1 < args.Length)
                        pngFiles.Add(args[++i]);
                    break;
            }
        }

        if (pngFiles.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine("PNG parsing requires -o / --output (ASM output path).");
                return;
            }

            ProjectManifest manifest;

            if (!string.IsNullOrWhiteSpace(manifestPath))
            {
                if (!File.Exists(manifestPath))
                {
                    Console.WriteLine("Manifest file not found.");
                    return;
                }

                manifest = JsonSerializer.Deserialize<ProjectManifest>(
                    File.ReadAllText(manifestPath),
                    SharpieJsonContext.Default.ProjectManifest
                )!;
            }
            else
            {
                manifest = new ProjectManifest(
                    "",
                    "",
                    "",
                    output,
                    false,
                    ProjectManifest.DefaultPalette(),
                    Constants.BiosVersion
                );
            }

            try
            {
                Helpers.BatchPngToAssembly(pngFiles, manifest, output);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("PNG sprites successfully converted to assembly.");
                Console.ResetColor();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                Console.ResetColor();
            }

            return;
        }

        ProjectManifest romManifest;

        if (!string.IsNullOrWhiteSpace(manifestPath))
        {
            if (!File.Exists(manifestPath))
            {
                Console.WriteLine("Manifest file not found.");
                return;
            }

            romManifest = JsonSerializer.Deserialize<ProjectManifest>(
                File.ReadAllText(manifestPath),
                SharpieJsonContext.Default.ProjectManifest
            )!;
        }
        else
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(input))
                missing.Add("-i / --input");
            if (!firmware)
            {
                if (string.IsNullOrWhiteSpace(title) && !firmware)
                    missing.Add("-t / --title");
                if (string.IsNullOrWhiteSpace(author) && !firmware)
                    missing.Add("-a / --author");
            }

            if (missing.Count > 0)
            {
                Console.WriteLine("Missing required arguments: " + string.Join(", ", missing));
                return;
            }

            romManifest = new ProjectManifest(
                !firmware ? title : "",
                !firmware ? author : "",
                input,
                string.IsNullOrWhiteSpace(output)
                    ? Path.ChangeExtension(input, firmware ? ".bin" : ".shr")
                    : output,
                firmware,
                ProjectManifest.DefaultPalette(),
                Constants.BiosVersion
            );
        }

        var validation = romManifest.Validate(Constants.BiosVersion);
        if (!validation.IsValid)
        {
            Console.WriteLine("Manifest validation failed:");
            foreach (var e in validation.Errors)
                Console.WriteLine(e);
            return;
        }

        try
        {
            AssembleRom(
                romManifest.InputPath,
                romManifest.Title,
                romManifest.Author,
                romManifest.ResolveOutputPath(),
                romManifest.Palette,
                romManifest.IsFirmware
            );

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                romManifest.IsFirmware
                    ? "Firmware build successful!"
                    : "Cartridge export successful!"
            );
            Console.ResetColor();
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            Console.ResetColor();
        }

        if (!string.IsNullOrWhiteSpace(manifestDumpPath))
        {
            try
            {
                if (!manifestDumpPath.EndsWith(".json"))
                    throw new FormatException("Output path is not a JSON file.");

                var json = JsonSerializer.Serialize(
                    romManifest,
                    SharpieJsonContext.Default.ProjectManifest
                );

                File.WriteAllText(manifestDumpPath, json);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Project manifest saved successfully!");
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
        var asm = new Assembler(isFirmware);
        var rom = asm.LoadFile(inputFilePath);

        var list = defaultPalette.ToList();

        while (defaultPalette.Length < 32)
        {
            list.Add(0xFF);
        }
        while (defaultPalette.Length > 32)
        {
            list.RemoveAt(list.Count - 1);
        }
        defaultPalette = list.ToArray();

        var exporter = new Exporter(romTitle, romAuthor, outputFilePath, defaultPalette);
        exporter.ExportRom(rom, isFirmware);
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
