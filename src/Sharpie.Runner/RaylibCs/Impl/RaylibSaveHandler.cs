#pragma warning disable CA2208 // Instantiate argument exceptions correctly
using Sharpie.Core.Drivers;

namespace Sharpie.Runner.RaylibCs.Impl;

public class RaylibSaveHandler : ISaveHandler
{
    public string? SavePath { get; set; }

    public void SaveToDisk(ReadOnlySpan<byte> saveRam)
    {
        Console.WriteLine($"Saving to {SavePath}");
        if (string.IsNullOrWhiteSpace(SavePath))
            throw new ArgumentNullException(nameof(SavePath), "No save path defined.");

        File.WriteAllBytes(SavePath, saveRam);
        Console.WriteLine($"Successfully wrote save data to {SavePath}");
    }

    public ReadOnlySpan<byte> LoadSaveData(ushort byteAmount)
    {
        Console.WriteLine("Loading");
        if (string.IsNullOrWhiteSpace(SavePath))
            throw new ArgumentNullException(nameof(SavePath), "No save path defined.");
        if (!File.Exists(SavePath))
            return [];

        var data = File.ReadAllBytes(SavePath);
        var totalAmount = data.Length < byteAmount ? data.Length : byteAmount;
        return data.AsSpan(0, totalAmount);
    }
}
