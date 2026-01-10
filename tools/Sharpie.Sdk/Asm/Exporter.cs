using System.Text;

namespace Sharpie.Sdk.Asm;

public class Exporter
{
    private readonly string _title;
    private readonly string _author;
    private readonly ushort _biosVersion = Meta.Constants.VersionBinFormat;
    private readonly string _fileName;

    private readonly byte[] _palette = new byte[16];

    public Exporter(string title, string author, string fileName, byte[] palette)
    {
        _title = title;
        _author = author;
        _fileName = fileName;
        for (int i = 0; i < 16; i++)
        {
            if (i >= palette.Length)
                _palette[i] = 0xFF;
            else
                _palette[i] = palette[i]; // can write anything, but the bootloader will ignore values > 31
        }
    }

    public void ExportRom(byte[] romData, bool asFirmware = false)
    {
        Console.WriteLine($"Exporter: Saving to {_fileName}...");
        using var fs = new FileStream(_fileName, FileMode.Create);
        using var writer = new BinaryWriter(fs);

        if (!asFirmware)
        {
            writer.Write(Encoding.ASCII.GetBytes("SHRP"));
            writer.Write(PadText(_title, 24));
            writer.Write(PadText(_author, 14));

            writer.Write(_biosVersion);
            writer.Write(CalculateChecksum(romData));

            writer.Write(_palette);
        }
        writer.Write(romData);
    }

    private byte[] PadText(string text, int maxLength)
    {
        var bytes = Encoding.ASCII.GetBytes(text ?? "");
        if (bytes.Length > maxLength)
            Console.WriteLine(
                $"Text {text} is too large for the allowed size of {maxLength}, truncating..."
            );
        var padded = new byte[maxLength];
        Array.Copy(bytes, padded, Math.Min(bytes.Length, maxLength));
        return padded;
    }

    private static uint CalculateChecksum(byte[] data)
    {
        long sum = 0;
        for (int i = 0; i < data.Length; i += 2)
        {
            var low = data[i];
            var high = (i + 1 < data.Length ? data[i + 1] : (byte)0);

            var word = (ushort)((high << 8) | low);
            sum += word;
        }

        return (uint)(~sum);
    }
}
