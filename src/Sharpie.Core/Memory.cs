namespace Sharpie.Core;

/// <summary>
/// The memory of the Sharpie console.
/// </summary>
public class Memory
{
    // Memory map:
    public const ushort RomStart = 0x0000; // 0x0000-0x7FFF Cartridge rom (exactly 32KB)
    public const ushort VramStart = 0x8000; // 0x8000-0xDFFF VRAM (exactly 24KB)
    // Note that the vram fits the entire screen. No framebuffer allowed.
    public const ushort AudioRamStart = 0xE000; // 0xE000-0xEFFF Audio Ram (4KB)
    public const ushort WorkRamStart = 0xF000; // 0xF000-0xFFFF Work Ram (4KB)
    public const ushort ColorPaletteStart = 0xFF00; //0xFF00-0xFF0F is the color palette, mapped within Work Ram.
    // 16 bytes, indexed. The next 224(?) bytes (0xFF0F-0xFFFF) are just insurance (boot mode flags, expansion, etc.)

    // The actual RAM chip
    private readonly byte[] _ram = new byte[65536];

    public byte ReadByte(ushort address)
    {
        return _ram[address]; //ushort caps at last ram index anyways
    }

    public void WriteByte(ushort address, byte value)
    {
        _ram[address] = value;
    }

    public ushort ReadWord(ushort address)
    {
        var lowByte = _ram[address];
        var highByte = _ram[address+1];

        return (ushort)((highByte << 8) & lowByte);
    }
    
    public void WriteWord(ushort address, ushort value)
    {
        var lowByte = (byte)(value & 0xFF); //bitmask not strictly necessary but oh well
        var highByte = (byte)((value >> 8) & 0xFF); //again

        _ram[address] = lowByte;
        _ram[address+1] = highByte;
    }

    // Good for dumping roms to memory
    public void LoadData(ushort startAddress, byte[] data)
    {
        Array.Copy(data, 0, _ram, startAddress, data.Length);
    }
}
