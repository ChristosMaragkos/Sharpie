namespace Sharpie.Core;

/// <summary>
/// The memory of the Sharpie console.
/// </summary>
public class Memory
{
    // Memory map:
    public const ushort RomStart = 0x0000;
    public const ushort SpriteAtlasStart = 0xDFFF;
    public const ushort OamStart = 0xE000;
    public const ushort WorkRamStart = 0xE800;

    // Note that the color palette is the last 16 memory slots of WRAM.
    public const ushort ColorPaletteStart = 0xEFF0; // 16 memory slots, one per color. Indexed as pointers to internal colors.
    public const ushort AudioRamStart = 0xF000; // 4KB of Audio Ram. No samples. Deal with it.

    // The actual RAM chip
    private readonly byte[] _contents = new byte[65536];

    /// <summary>
    /// Reads a byte from memory as an 8 bit integer
    /// </summary>
    /// <param name="address">The address of the byte, usually Program Counter + 1</param>
    public byte ReadByte(ushort address)
    {
        return _contents[address]; //ushort caps at last ram index anyways
    }

    public byte ReadByte(int address) => ReadByte((ushort)address);

    public void WriteByte(ushort address, byte value)
    {
        _contents[address] = value;
    }

    public void WriteByte(int address, byte value) => WriteByte((ushort)address, value);

    /// <summary>
    /// Formats two bytes into a 16 bit integer from memory. Little Endian.
    /// </summary>
    /// <param name="address">The address of the low byte of the word, usually Program Counter + 1</param>
    public ushort ReadWord(ushort address)
    {
        var lowByte = _contents[address];
        var highByte = _contents[address + 1];

        return (ushort)((highByte << 8) | lowByte);
    }

    public ushort ReadWord(int address) => ReadWord((ushort)address);

    public void WriteWord(ushort address, ushort value)
    {
        var lowByte = (byte)(value & 0xFF); //bitmask not strictly necessary but oh well
        var highByte = (byte)((value >> 8) & 0xFF); //again

        _contents[address] = lowByte;
        _contents[address + 1] = highByte;
    }

    public void WriteWord(int address, ushort value) => WriteWord((ushort)address, value);

    // Good for dumping roms to memory
    public void LoadData(ushort startAddress, byte[] data)
    {
        Array.Copy(data, 0, _contents, startAddress, data.Length);
    }

    public void ClearRange(int from, int amount) => Array.Clear(_contents, from, amount);

    public Span<byte> Slice(int from, int amount) => _contents.AsSpan(from, amount);
}
