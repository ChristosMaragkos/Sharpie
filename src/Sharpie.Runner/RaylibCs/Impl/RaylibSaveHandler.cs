using Sharpie.Core.Drivers;

namespace Sharpie.Runner.RaylibCs.Impl;

public class RaylibSaveHandler : ISaveHandler
{
    public string? SavePath { get; set; }

    public void SaveToDisk(ReadOnlySpan<byte> saveRam)
    {
        if (string.IsNullOrWhiteSpace(SavePath))
            throw new ArgumentNullException(nameof(SavePath), "No save path defined.");
        var directoryName =
            Path.GetDirectoryName(SavePath) ?? throw new ArgumentNullException(nameof(SavePath));
        Directory.CreateDirectory(directoryName);

        File.WriteAllBytes(SavePath, saveRam);
        Console.WriteLine($"Successfully wrote save data to {SavePath}");
    }
}
