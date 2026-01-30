namespace Sharpie.Core.Hardware;

internal class Memory
{
    // Memory map
    public const ushort RomStart = 0x0000;
    public const ushort FixedRomEnd = 0x47FF; // 18 KiB fixed region
    public const ushort SwitchableRomStart = 0x4800; // 32 KiB swappable region
    public const ushort SwitchableRomEnd = 0xC7FF; // right before sprite atlas
    public const ushort SpriteAtlasStart = 0xE7FF; // grows downward, always loaded
    public const ushort SpriteAtlasBottom = 0xC800;
    public const ushort WorkRamStart = 0xE800;
    public const ushort AudioRamStart = 0xF800;
    public const ushort SaveRamStart = AudioRamStart + 32;
    public const ushort ReservedSpaceStart = 0xF800 + 544;
    public const ushort ColorPaletteStart = 0xFFE0;

    private readonly byte[] _contents;

    private byte[][]? _banks;
    private int _currentBankIndex;
    public int BankCount => _banks != null ? _banks.Length : 0;

    public Memory(ushort lastAddress = ushort.MaxValue)
    {
        _contents = new byte[lastAddress + 1];
    }

    public void SetBanks(byte[][] banks)
    {
        _banks = banks;
        _currentBankIndex = 0;
    }

    public void SelectBank(int index)
    {
        if (_banks == null)
            return; // ignore if banks aren't initialized
        _currentBankIndex = index;
    }

    private static bool IsSwitchableRegion(ushort address) =>
        address >= SwitchableRomStart && address <= SwitchableRomEnd;

    public byte ReadByte(ushort address)
    {
        if (_banks != null && IsSwitchableRegion(address))
        {
            var offset = address - SwitchableRomStart;
            return _banks[_currentBankIndex][offset];
        }

        return _contents[address];
    }

    public byte ReadByte(int address) => ReadByte((ushort)address);

    public void WriteByte(ushort address, byte value)
    {
        if (_banks != null && IsSwitchableRegion(address))
        {
            var offset = address - SwitchableRomStart;
            _banks[_currentBankIndex][offset] = value;
            return;
        }

        _contents[address] = value;
    }

    public void WriteByte(int address, byte value) => WriteByte((ushort)address, value);

    public ushort ReadWord(ushort address)
    {
        var lowByte = ReadByte(address);
        var highByte = ReadByte((ushort)(address + 1));
        return (ushort)((highByte << 8) | lowByte);
    }

    public ushort ReadWord(int address) => ReadWord((ushort)address);

    public void WriteWord(ushort address, ushort value)
    {
        WriteByte(address, (byte)(value & 0xFF));
        WriteByte((ushort)(address + 1), (byte)((value >> 8) & 0xFF));
    }

    public void WriteWord(int address, ushort value) => WriteWord((ushort)address, value);

    public void LoadData(ushort startAddress, byte[] data)
    {
        Array.Copy(data, 0, _contents, startAddress, data.Length);
    }

    public void ClearRange(int from, int amount) => Array.Clear(_contents, from, amount);

    public void FillRange(int startIndex, int amount, byte value) =>
        Array.Fill(_contents, value, startIndex, amount);

    public Span<byte> Slice(int from, int amount) => _contents.AsSpan(from, amount);

    public ReadOnlySpan<byte> View(int from, int amount)
    {
        if ((uint)from + (uint)amount > (uint)_contents.Length)
        {
            Console.WriteLine(
                "Attempted to access memory address that was out of range for this instance of the Memory class."
            );
            return new ReadOnlySpan<byte>(new byte[amount]); // Return an empty span instead of throwing an exception
        }
        return new ReadOnlySpan<byte>(_contents, from, amount);
    }
}
