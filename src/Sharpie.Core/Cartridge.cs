using System.Text;
using Sharpie.Core.Hardware;

namespace Sharpie.Core;

public sealed class Cartridge
{
    public byte[] RomData { get; init; } = new byte[59392];
    public string Title { get; init; } = "";
    public ushort MinBiosVersion { get; init; } = 0;
    public byte[] Palette { get; init; } = new byte[16];

    public static Cartridge Load(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        using var reader = new BinaryReader(fs);

        if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "SHRP")
            throw new FormatException("Not a valid Sharpie ROM.");

        var title = Encoding.ASCII.GetString(reader.ReadBytes(24)).TrimEnd('\0');

        var author = Encoding.ASCII.GetString(reader.ReadBytes(14)).TrimEnd('0');

        var minVersion = reader.ReadUInt16();
        if (minVersion > IMotherboard.VersionBinFormat) // safely fail if we try to load newer cartridge on older firmware
            throw new ApplicationException(
                $"ROM {title} requires BIOS version {minVersion}. Current BIOS version: {IMotherboard.VersionBinFormat}"
            );

        var checksum = reader.ReadUInt32();

        var palette = reader.ReadBytes(16);

        fs.Seek(0x40, SeekOrigin.Begin);
        var rom = reader.ReadBytes((int)(fs.Length - fs.Position));

        return new Cartridge
        {
            Title = title,
            MinBiosVersion = minVersion,
            Palette = palette,
            RomData = rom,
        };
    }
}
