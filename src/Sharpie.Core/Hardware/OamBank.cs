namespace Sharpie.Core.Hardware;

// This was gonna be an implementation of an interface but virtual dispatch
// made the emulator crawl. Who would'a thought?
internal class OamBank
{
    public const int OamEntrySize = 7;
    public const int Size = 8192;
    public const int MaxEntries = Size / OamEntrySize;
    public const int MaxHudEntries = 512;

    private readonly byte[] _contents = new byte[Size];

    public int Cursor
    {
        get;
        set { field = value < MaxEntries ? value : 0; } // when did the field keyword leave preview?
    } = 0;

    public void WriteEntry(ushort x, ushort y, byte tileId, byte attr, byte type)
    {
        var startAddress = Cursor * OamEntrySize;
        WriteWord(startAddress, x);
        WriteWord(startAddress + 2, y);
        WriteByte(startAddress + 4, tileId);
        WriteByte(startAddress + 5, attr);
        WriteByte(startAddress + 6, type);

        Cursor++;
    }

    public (ushort X, ushort Y, byte TileId, byte Attr, byte Type) ReadEntry(int index)
    {
        var addr = index * OamEntrySize;
        return (
            ReadWord(addr),
            ReadWord(addr + 2),
            ReadByte(addr + 4),
            ReadByte(addr + 5),
            ReadByte(addr + 6)
        );
    }

    [Flags]
    public enum SpriteFlags : byte
    {
        /// Horizontal Flip, bit 0
        FlipH = 1,

        /// Vertical Flip, bit 1
        FlipV = 2,

        /// HUD toggle - ignore collision, ignore camera, draw with screen coordinates, bit 2
        Hud = 4,

        /// Background toggle - ignore collision, bit 3
        Background = 8,
    }

    private byte ReadByte(int address)
    {
        return _contents[address];
    }

    private void WriteByte(int address, byte value)
    {
        _contents[address] = value;
    }

    private ushort ReadWord(int address)
    {
        var lowByte = _contents[address];
        var highByte = _contents[address + 1];

        return (ushort)((highByte << 8) | lowByte);
    }

    private void WriteWord(int address, ushort value)
    {
        var lowByte = (byte)(value & 0xFF);
        var highByte = (byte)((value >> 8) & 0xFF);

        _contents[address] = lowByte;
        _contents[address + 1] = highByte;
    }

    public void Invalidate(int from, int to) =>
        Array.Fill(_contents, (byte)0xFF, from, to - from + 1);

    public void InvalidateAll() => Array.Fill(_contents, (byte)0xFF);
}
