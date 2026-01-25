using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sharpie.Sdk.Meta;

public sealed class ProjectManifest
{
    public string Title { get; set; } = "Untitled";
    public string Author { get; set; } = "Unknown";

    [JsonRequired]
    public string InputPath { get; set; } = null!;

    public string OutputPath { get; set; }
    public bool IsFirmware { get; set; }
    public int[] Palette { get; set; } = DefaultPalette();

    [JsonConverter(typeof(BiosVersionConverter))]
    public Version MinimumBiosVersion { get; set; } = Constants.BiosVersion;

    [JsonConstructor]
    public ProjectManifest(
        string title,
        string author,
        string inputPath,
        string outputPath,
        bool isFirmware,
        int[] palette,
        Version minimumBiosVersion
    )
    {
        Title = title;
        Author = author;
        InputPath = inputPath;
        OutputPath = outputPath;
        IsFirmware = isFirmware;
        Palette = palette;
        MinimumBiosVersion = minimumBiosVersion;
    }

    /// <summary>
    /// Returns a value that indicates whether this is a valid project manifest instance.
    /// We don't throw here on purpose so different modes (e.g. GUI vs CLI)
    /// can handle errors differently.
    /// </summary>
    public ManifestValidationResult Validate(Version currentBiosVersion)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(InputPath))
            errors.Add("Input Path is required.");

        if (!File.Exists(InputPath))
            errors.Add($"Input file does not exist: {InputPath}");

        if (!InputPath.EndsWith(".asm"))
            errors.Add("Input file is not a .asm file.");

        if (Palette is null || Palette.Length != 32)
            errors.Add("Palette must contain exactly 32 bytes.");

        if (MinimumBiosVersion is null)
            errors.Add("Minimum BIOS version is required.");

        if (MinimumBiosVersion > currentBiosVersion)
            errors.Add(
                $"Manifest requires BIOS {MinimumBiosVersion}, but current BIOS is {currentBiosVersion}."
            );

        Palette ??= DefaultPalette();

        for (int i = 0; i < Palette.Length; i++)
        {
            if (Palette[i] < 0 || Palette[i] > 0xFF)
                Palette[i] = 0xFF;
        }

        var list = Palette.ToList();
        while (list.Count < 32)
            list.Add(0xFF);
        while (list.Count > 32)
            list.RemoveAt(list.Count - 1);

        Palette = list.ToArray();

        return errors.Count == 0
            ? ManifestValidationResult.Valid
            : ManifestValidationResult.Invalid(errors);
    }

    public string ResolveOutputPath()
    {
        if (!string.IsNullOrWhiteSpace(OutputPath))
            return OutputPath;

        var extension = IsFirmware ? ".bin" : ".shr";
        return Path.ChangeExtension(InputPath, extension);
    }

    public static int[] DefaultPalette() => Enumerable.Repeat(0xFF, 32).ToArray();
}

public class BiosVersionConverter : JsonConverter<Version>
{
    public override Version? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var str = reader.GetString();
        if (str == null)
            return null;

        var v = str.Split('.');
        if (v == null)
            return null;

        if (v.Length != 2)
            return null;

        if (!int.TryParse(v[0], out var major) || !int.TryParse(v[1], out var minor))
            return null;

        return new Version(major, minor);
    }

    public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value.Major}.{value.Minor}");
    }
}
