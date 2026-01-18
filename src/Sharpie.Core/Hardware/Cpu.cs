namespace Sharpie.Core.Hardware;

[Flags]
internal enum CpuFlags : ushort
{
    None = 0,
    Carry = 0x01,
    Zero = 0x02,
    Overflow = 0x04,
    Negative = 0x08,
}

internal partial class Cpu
{
    private const int MaxOamSlots = 512;
    private readonly IMotherboard _mobo;

    public Cpu(IMotherboard motherboard)
    {
        _mobo = motherboard;
        Reset();
    }

    private ushort _pc;
    private ushort[] _registers = new ushort[32];
    private int _registerBankOffset => ((FlagRegister & 0x8000) >> 15) == 1 ? 16 : 0;

    private ref ushort GetRegister(int index) => ref _registers[index + _registerBankOffset];

    private void FlipRegisterBanks()
    {
        FlagRegister ^= 0x8000;
    }

    private readonly Random _rng = new();
    private ushort _oamReg = 0;
    private ushort OamRegister
    {
        get => _oamReg;
        set => _oamReg = value < MaxOamSlots ? value : (ushort)0;
    }
    private byte[] _tagMap = new byte[512];
    private Stack<ushort> _callStack = new();

    private int _cursorPosX;
    private int CursorPosX
    {
        get => _cursorPosX;
        set
        {
            // Handle X wrapping
            int newX = value;

            // If X overflowed right (>= 32)
            while (newX >= 32)
            {
                newX -= 32;
                CursorPosY++; // Recursively calls Y setter
            }

            // If X underflowed left (< 0) - Backspace logic
            while (newX < 0)
            {
                newX += 32;
                CursorPosY--; // Recursively calls Y setter
            }

            _cursorPosX = newX;
        }
    }

    private int _cursorPosY;
    private int CursorPosY
    {
        get => _cursorPosY;
        set
        {
            // Handle Y wrapping (0-31)
            int newY = value;

            // Standard modulo doesn't handle negatives well, so we helper it:
            // This ensures -1 becomes 31
            _cursorPosY = ((newY % 32) + 32) % 32;
        }
    }

    // layout (right to left):
    // 0-Carry (unsigned overflow, result >=65535 or < 0), 0x01
    // 1-Zero (result exactly zero), 0x02
    // 2-Overflow (signed), positive + positive = negative, 0x04
    // 3-Negative (highest bit is 1), 0x08
    // 15 (MSB) - Register Bank (are we using registers 0-15 or 16-31?)
    private ushort FlagRegister;

    private void UpdateFlags(int result, ushort op1, ushort op2, bool subtraction = false)
    {
        FlagRegister &= 0xFFF0;
        ushort flags = 0;

        if (!subtraction)
        {
            if (result > ushort.MaxValue)
                flags |= (ushort)CpuFlags.Carry;
        }
        else
        {
            if (op1 < op2)
                flags |= (ushort)CpuFlags.Carry;
        }

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
    public bool IsAwaitingVBlank { get; set; }

    public void Halt() => IsHalted = true;

    public void AwaitVBlank() => IsAwaitingVBlank = true;

    public void Reset()
    {
        Array.Clear(_registers, 0, _registers.Length);
        _cursorPosX = 0;
        _cursorPosY = 0;
        _pc = 0;
        FlagRegister = 0;
        _tagMap = new byte[512];
        _callStack = new();
        OamRegister = 0;
        LoadDefaultPalette();
        IsHalted = false;
        _resetRequested = false;
    }

    public void LoadDefaultPalette()
    {
        for (byte i = 0; i < 16; i++)
        {
            _mobo.WriteByte(Memory.ColorPaletteStart + i, i);
        }
    }

    public void LoadPalette(byte[] colorPalette)
    {
        for (byte i = 0; i < 16; i++)
        {
            _mobo.WriteByte(
                Memory.ColorPaletteStart + i,
                colorPalette[i] > 0x1F ? i : colorPalette[i]
            );
        }
    }

    private bool _resetRequested;

    public void RequestReset() => _resetRequested = true;

    public void Cycle()
    {
        if (IsHalted)
            return;

        byte opcode = _mobo.ReadByte(_pc);

        ExecuteOpcode(opcode, out ushort pcDelta);

        if (_resetRequested)
        {
            Reset();
            return;
        }

        if (IsHalted) // did we just halt?
            return;
        Advance(pcDelta);
    }

    private void Advance(int pcDelta)
    {
        if (IsHalted) // one more for good measure
            return;
        _pc += (ushort)pcDelta;
    }

    public override string ToString()
    {
        return @$"
===SHARP-16 CPU===
PC: 0x{_pc:X4}
Registers:
    r0  -> {_registers[0]}
    r1  -> {_registers[1]}
    r2  -> {_registers[2]}
    r3  -> {_registers[3]}
    r4  -> {_registers[4]}
    r5  -> {_registers[5]}
    r6  -> {_registers[6]}
    r7  -> {_registers[7]}
    r8  -> {_registers[8]}
    r9  -> {_registers[9]}
    r10 -> {_registers[10]}
    r11 -> {_registers[11]}
    r12 -> {_registers[12]}
    r13 -> {_registers[13]}
    r14 -> {_registers[14]}
    r15 -> {_registers[15]}
    r16 -> {_registers[16]}
    r17 -> {_registers[17]}
    r18 -> {_registers[18]}
    r19 -> {_registers[19]}
    r20 -> {_registers[20]}
    r21 -> {_registers[21]}
    r22 -> {_registers[22]}
    r23 -> {_registers[23]}
    r24 -> {_registers[24]}
    r25 -> {_registers[25]}
    r26 -> {_registers[26]}
    r27 -> {_registers[27]}
    r28 -> {_registers[28]}
    r29 -> {_registers[29]}
    r30 -> {_registers[30]}
    r31 -> {_registers[31]}
";
    }

    /// <summary>
    /// Reads the next memory address and formats it to two register indices
    /// </summary>
    private (int highNibble, int lowNibble) ReadRegisterArgs(int offset = 1)
    {
        var args = _mobo.ReadByte((_pc + offset));
        var rX = (args >> 4) & 0x0F;
        var rY = args & 0x0F;

        return (rX, rY);
    }

    private static int IndexFromOpcode(byte opcode)
    {
        return opcode & 0x0F;
    }
}
