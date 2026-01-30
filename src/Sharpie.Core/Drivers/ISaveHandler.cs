namespace Sharpie.Core.Drivers;

public interface ISaveHandler
{
    string? SavePath { get; }
    void SaveToDisk(ReadOnlySpan<byte> saveRam);
}
