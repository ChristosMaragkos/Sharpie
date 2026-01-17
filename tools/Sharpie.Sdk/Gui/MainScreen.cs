using System.Numerics;
using System.Text.Json;
using ImGuiNET;
using Raylib_cs;
using Sharpie.Sdk.Meta;
using Sharpie.Sdk.Serialization;
using TinyDialogsNet;

namespace Sharpie.Sdk.Gui;

public class MainScreen
{
    private static ProjectManifest manifest = new("", "", "", "", false, [], Constants.BiosVersion);
    private static string errorMsg = "";
    private static bool successfulBuild = true;

    private static bool showPaletteEditor = false;
    private static List<int> selectedColors = new() { 0 };

    public static void RunMainGui()
    {
        Raylib.InitWindow(450, 600, "Sharpie SDK");
        rlImGui.Setup();

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();

            rlImGui.Begin();

            DrawGui();

            rlImGui.End();

            Raylib.EndDrawing();
        }

        rlImGui.Shutdown();
        Raylib.CloseWindow();
    }

    private static void DrawGui()
    {
        var flags =
            ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoBringToFrontOnFocus;

        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight()));
        ImGui.Begin("RootWindow", flags);

        float spacing = 5f;

        ImGui.BeginChild("MetadataSection", new Vector2(0, 150), ImGuiChildFlags.None);
        ImGui.Text("ROM Metadata");
        ImGui.Separator();

        var title = manifest.Title;
        if (ImGui.InputText("Title", ref title, 64))
            manifest.Title = title;

        var romAuthor = manifest.Author;
        if (ImGui.InputText("Author", ref romAuthor, 64))
            manifest.Author = romAuthor;

        if (ImGui.Button("Import Project..."))
        {
            var selected = TinyDialogs.OpenFileDialog(
                "Select Project Manifest",
                Directory.GetCurrentDirectory(),
                false,
                new FileFilter("JSON Files", ["*.json"])
            );
            if (!selected.Canceled && selected.Paths.Any())
            {
                try
                {
                    manifest = JsonSerializer.Deserialize<ProjectManifest>(
                        File.ReadAllText(selected.Paths.First()),
                        SharpieJsonContext.Default.ProjectManifest
                    )!; // better hope it catches null
                    errorMsg = "Project imported successfully!";
                    successfulBuild = true;
                }
                catch (Exception e)
                {
                    errorMsg = $"Error importing project:\n{e.Message}";
                    successfulBuild = false;
                }
            }
        }

        if (ImGui.Button("Edit Palette"))
            showPaletteEditor = true;

        DrawPaletteEditor();

        ImGui.EndChild();

        ImGui.Dummy(new Vector2(0, spacing));

        ImGui.BeginChild("FileSection", new Vector2(0, 175), ImGuiChildFlags.None);

        ImGui.Text("File Selection & Exporting");
        ImGui.Separator();
        if (ImGui.Button("Select Project File..."))
        {
            var selected = TinyDialogs.OpenFileDialog(
                "Select Assembly File",
                Directory.GetCurrentDirectory(),
                false,
                new FileFilter("ASM files", ["*.asm"])
            );
            if (!selected.Canceled && selected.Paths.Any())
            {
                manifest.InputPath = selected.Paths.First();
                manifest.OutputPath = "";
                manifest.OutputPath = manifest.ResolveOutputPath();
            }
        }

        ImGui.SameLine();
        var remaining = ImGui.GetContentRegionAvail().X;
        ImGui.SetNextItemWidth(remaining);

        var inputPath = manifest.InputPath;
        if (ImGui.InputText("##InputPath", ref inputPath, 255))
            manifest.InputPath = inputPath;

        ImGui.Dummy(new Vector2(0, spacing));

        if (ImGui.Button("Select Output Directory"))
        {
            var selected = TinyDialogs.SaveFileDialog(
                "Select Output Path",
                manifest.ResolveOutputPath(),
                new FileFilter(
                    manifest.IsFirmware ? "BIN files" : "SHR files",
                    manifest.IsFirmware ? [".bin"] : [".shr"]
                )
            );
            if (!selected.Canceled && !string.IsNullOrWhiteSpace(selected.Path))
            {
                manifest.OutputPath = selected.Path;
            }
        }

        ImGui.SameLine();
        remaining = ImGui.GetContentRegionAvail().X;
        ImGui.SetNextItemWidth(remaining);

        var outputPath = manifest.OutputPath;
        if (ImGui.InputText("##OutputPath", ref outputPath, 255))
            manifest.OutputPath = outputPath;

        ImGui.Dummy(new Vector2(0, spacing));

        if (ImGui.Button("Export..."))
        {
            TryAssemble();
        }

        ImGui.SameLine();
        var fw = manifest.IsFirmware;
        if (ImGui.Checkbox("Export as Firmware?", ref fw))
        {
            manifest.IsFirmware = fw;
            manifest.OutputPath = Path.ChangeExtension(manifest.OutputPath, fw ? ".bin" : ".shr");
        }

        if (ImGui.Button("Export Manifest..."))
        {
            var selected = TinyDialogs.SaveFileDialog(
                "Save Project Manifest",
                manifest.InputPath,
                new FileFilter("JSON files", ["*.json"])
            );
            if (!selected.Canceled && !string.IsNullOrWhiteSpace(selected.Path))
            {
                try
                {
                    var json = JsonSerializer.Serialize(
                        manifest,
                        SharpieJsonContext.Default.ProjectManifest
                    );
                    File.WriteAllText(selected.Path, json);
                    errorMsg = "Manifest exported successfully!";
                    successfulBuild = true;
                }
                catch (Exception e)
                {
                    errorMsg = e.Message;
                    successfulBuild = false;
                }
            }
        }

        ImGui.EndChild();

        ImGui.Dummy(new Vector2(0, spacing * 2));

        // --- Editors Section ---
        ImGui.BeginChild("EditorsSection", new Vector2(0, 80), ImGuiChildFlags.None);

        ImGui.Text("More Editors");
        ImGui.Separator();
        if (ImGui.Button("Sprite Editor"))
            Console.WriteLine("Opening Sprite Editor");
        ImGui.SameLine();
        if (ImGui.Button("Music Editor"))
            Console.WriteLine("Opening Music Editor");

        ImGui.EndChild();

        ImGui.BeginChild("ErrorSection");
        ImGui.Text("Build Output");
        ImGui.Separator();
        ImGui.TextColored(
            successfulBuild ? new Vector4(0, 255, 0, 255) : new Vector4(255, 0, 0, 255),
            errorMsg
        );
        ImGui.EndChild();

        ImGui.End();
    }

    private static void TryAssemble()
    {
        var valid = manifest.Validate(Constants.BiosVersion);
        if (!valid.IsValid)
        {
            errorMsg = valid.Errors.First();
            successfulBuild = false;
            return;
        }
        try
        {
            Program.AssembleRom(
                manifest.InputPath,
                manifest.Title,
                manifest.Author,
                manifest.OutputPath,
                manifest.Palette,
                manifest.IsFirmware
            );
            errorMsg = "Build Successful!";
            successfulBuild = true;
        }
        catch (Exception e)
        {
            errorMsg = e.Message;
            successfulBuild = false;
        }
    }

    private static void DrawPaletteEditor()
    {
        if (!showPaletteEditor)
            return;

        int cols = 16;
        int rows = 2;
        float squareSize = 24f;

        Vector2 windowSize = new Vector2(cols * squareSize + 30, rows * squareSize + 80); // +padding for title & borders
        ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);
        ImGui.Begin("Palette Editor", ref showPaletteEditor, ImGuiWindowFlags.NoMove);
        var drawList = ImGui.GetWindowDrawList();
        var winPos = ImGui.GetCursorScreenPos();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int idx = row * cols + col;
                var (r, g, b) = Constants.MasterPalette[idx];
                Vector4 colVec = new(r / 255f, g / 255f, b / 255f, 1f);

                var rectMin = new Vector2(winPos.X + col * squareSize, winPos.Y + row * squareSize);
                var rectMax = rectMin + new Vector2(squareSize, squareSize);

                // draw the square
                drawList.AddRectFilled(rectMin, rectMax, ImGui.ColorConvertFloat4ToU32(colVec), 0f);

                // draw outline if selected
                if (selectedColors.Contains(idx))
                {
                    drawList.AddRect(
                        rectMin,
                        rectMax,
                        ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.5f, 0.5f, 1f)),
                        0f,
                        ImDrawFlags.None,
                        3f
                    );

                    var label = selectedColors.IndexOf(idx).ToString();
                    var textSize = ImGui.CalcTextSize(label);
                    var textPos = rectMin + (new Vector2(squareSize, squareSize) - textSize) * 0.5f;

                    var luminance = 0.299f * r + 0.299f * g + 0.299f * b;
                    var textCol =
                        luminance < 128 ? new Vector4(1f, 1f, 1f, 1f) : new Vector4(0f, 0f, 0f, 1f);
                    drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(textCol), label);
                }

                // handle clicks manually
                var mouse = ImGui.GetIO().MousePos;
                bool hovered =
                    mouse.X >= rectMin.X
                    && mouse.X <= rectMax.X
                    && mouse.Y >= rectMin.Y
                    && mouse.Y <= rectMax.Y;
                if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left) && idx != 0)
                {
                    if (selectedColors.Contains(idx))
                        selectedColors.Remove(idx);
                    else if (selectedColors.Count < 16)
                        selectedColors.Add(idx);
                }
            }
        }

        ImGui.Dummy(new Vector2(0, rows * squareSize)); // reserve space

        ImGui.Columns(1);
        if (ImGui.Button("Apply Changes"))
        {
            var paletteBytes = new int[16];
            for (int j = 0; j < 16; j++)
                paletteBytes[j] = j < selectedColors.Count ? selectedColors[j] : 0xFF;

            manifest.Palette = paletteBytes;
            showPaletteEditor = false;
        }

        ImGui.End();
    }
}
