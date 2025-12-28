namespace Sharpie.Core;

[Flags]
internal enum CpuFlags : ushort
{
    None = 0,
    Carry = 0x01,
    Zero = 0x02,
    Overflow = 0x04,
    Negative = 0x08,
}

public partial class Cpu
{
    private const int MaxOamSlots = 512;
    private readonly Memory _memory;
    private readonly IMotherboard _mobo;

    public Cpu(Memory memory, IMotherboard motherboard)
    {
        _memory = memory;
        _mobo = motherboard;
        Reset();
    }

    private ushort _pc;
    private ushort _sp;
    private ushort[] _registers = new ushort[16];
    private readonly Random _rng = new();
    private ushort _oamReg = Memory.OamStart;
    private ushort OamRegister
    {
        get => _oamReg;
        set => _oamReg = value < MaxOamSlots ? value : (ushort)0;
    }
    private byte[] _tagMap = new byte[512];

    private int _cursorPosX;
    private int CursorPosX
    {
        get => _cursorPosX;
        set => _cursorPosX = Math.Clamp(_cursorPosX, 0, 31);
    }

    private int _cursorPosY;
    private int CursorPosY
    {
        get => _cursorPosY;
        set => _cursorPosY = Math.Clamp(_cursorPosY, 0, 31);
    }

    // layout (right to left):
    // 0-Carry (unsigned overflow, result >=65535 or < 0), 0x01
    // 1-Zero (result exactly zero), 0x02
    // 2-Overflow (signed), positive + positive = negative, 0x04
    // 3-Negative (highest bit is 1), 0x08
    private ushort FlagRegister;

    private void UpdateFlags(int result, ushort op1, ushort op2, bool subtraction = false)
    {
        FlagRegister &= 0xFFF0;
        ushort flags = 0;

        if (result > ushort.MaxValue || result < 0)
            flags |= (ushort)CpuFlags.Carry; // carry

        if ((ushort)result == 0)
            flags |= (ushort)CpuFlags.Zero; // zero

        var signedResult = (short)result;
        var signedOp1 = (short)op1;
        var signedOp2 = subtraction ? -(short)op2 : (short)op2;

        if (((signedOp1 ^ signedResult) & (signedOp2 ^ signedResult) & 0x8000) != 0) // what the fuck
            flags |= (ushort)CpuFlags.Overflow; // overflow

        if ((result & 0x8000) != 0)
            flags |= (ushort)CpuFlags.Negative;

        FlagRegister = flags;
    }

    private void UpdateLogicFlags(ushort result)
    {
        FlagRegister &= 0xFFF0;

        if (result == 0)
            FlagRegister |= (ushort)CpuFlags.Zero;

        if ((result & 0x8000) != 0)
            FlagRegister |= (ushort)CpuFlags.Negative;
    }

    private void SetFlag(bool value, CpuFlags flag)
    {
        if (value)
            FlagRegister |= (ushort)flag;
        else
            FlagRegister &= (ushort)~flag;
    }

    private bool IsFlagOn(CpuFlags flag)
    {
        return (FlagRegister & (ushort)flag) != 0;
    }

    public bool IsHalted { get; private set; }

    public void Halt() => IsHalted = true;

    public void Reset()
    {
        Array.Clear(_registers, 0, _registers.Length);

        _pc = Memory.RomStart;

        // 0xEFEF to not collide with color palette and above
        _sp = 0xEFEF;

        IsHalted = false;
    }

    public void LoadPalette(byte[] colorPalette)
    {
        for (int i = 0; i < 16; i++)
        {
            _memory.WriteByte(
                Memory.ColorPaletteStart + i,
                colorPalette[i] > 0x1F ? (byte)i : colorPalette[i]
            );
        }
    }

    public void Cycle()
    {
        if (IsHalted)
            return;

        byte opcode = _memory.ReadByte(_pc);

        ExecuteOpcode(opcode, out ushort pcDelta);

        Advance(pcDelta);
    }

    private void Advance(int pcDelta)
    {
        if (IsHalted)
            return;
        _pc += (ushort)pcDelta;
    }

    public override string ToString()
    {
        return @$"
===SHARP-16 CPU===
PC: 0x{_pc:X4}
SP: 0x{_sp:X4}
0p: 0x{_memory.ReadByte(_pc):X2}
";
    }

    /// <summary>
    /// Reads the next memory address and formats it to two register indices
    /// </summary>
    private (int x, int y) ReadRegisterArgs(int offset = 1)
    {
        var args = _memory.ReadByte((_pc + offset));
        var rX = (args >> 4) & 0x0F;
        var rY = args & 0x0F;

        return (rX, rY);
    }

#pragma warning disable CA1822 // Mark members as static
    private int IndexFromOpcode(byte opcode)
#pragma warning restore CA1822 // Mark members as static
    {
        return opcode & 0x0F;
    }
}
