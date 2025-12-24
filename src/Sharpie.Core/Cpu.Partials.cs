namespace Sharpie.Core;

public partial class Cpu
{
    private partial void Execute_MOV(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        _registers[x] = _registers[y];
    }

    private partial void Execute_LDM(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);
        var address = _memory.ReadWord((_pc + 1));
        _registers[x] = _memory.ReadWord(address);
    }

    private partial void Execute_LDI(byte opcode, ref ushort pcDelta)
    {
        Console.WriteLine("LDI");
        var x = IndexFromOpcode(opcode);
        var value = _memory.ReadWord(_pc + 1);
        _registers[x] = value;
    }

    private partial void Execute_STM(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);
        var address = _memory.ReadWord(_pc + 1);
        _memory.WriteWord(address, _registers[x]);
    }

    private partial void Execute_ADD(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var result = _registers[x] + _registers[y];
        UpdateFlags(result, _registers[x], _registers[y]);
        _registers[x] = (ushort)result;
    }

    private partial void Execute_SUB(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var result = _registers[x] - _registers[y];
        UpdateFlags(result, _registers[x], _registers[y], true);
        _registers[x] = (ushort)result;
    }

    private partial void Execute_MUL(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        int result = _registers[x] * _registers[y];
        var truncated = (ushort)result;

        UpdateLogicFlags(truncated);
        var dataLost = result > ushort.MaxValue;
        SetFlag(dataLost, CpuFlags.Carry);
        SetFlag(dataLost, CpuFlags.Overflow);
    }

    private partial void Execute_DIV(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();
        ushort valY = _registers[y];

        if (valY == 0)
        {
            _registers[x] = 0;

            FlagRegister &= 0xFFF0;
            SetFlag(true, CpuFlags.Zero);
            SetFlag(true, CpuFlags.Overflow);
            return;
        }

        var result = (ushort)(_registers[x] / valY);

        UpdateLogicFlags(result);

        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        _registers[x] = result;
    }

    private partial void Execute_MOD(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();
        ushort valY = _registers[y];

        if (valY == 0)
        {
            _registers[x] = 0;

            FlagRegister &= 0xFFF0;
            SetFlag(true, CpuFlags.Zero);
            SetFlag(true, CpuFlags.Overflow);
            return;
        }

        var result = (ushort)(_registers[x] % valY);
        UpdateLogicFlags(result);

        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        _registers[x] = result;
    }

    private partial void Execute_AND(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var result = (ushort)(_registers[x] & _registers[y]);
        UpdateLogicFlags(result);

        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        _registers[x] = result;
    }

    private partial void Execute_OR(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var result = (ushort)(_registers[x] | _registers[y]);
        UpdateLogicFlags(result);

        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        _registers[x] = result;
    }

    private partial void Execute_XOR(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var result = (ushort)(_registers[x] ^ _registers[y]);
        UpdateLogicFlags(result);

        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        _registers[x] = result;
    }

    private partial void Execute_SHL(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var shiftAmount = _registers[y] & 0x0F;
        var original = _registers[x];

        var result = _registers[x] << shiftAmount;
        var truncated = (ushort)result;

        UpdateLogicFlags(truncated);
        SetFlag(result > ushort.MaxValue, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        _registers[x] = truncated;
    }

    private partial void Execute_SHR(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var shiftAmount = _registers[y] & 0x0F;
        var original = _registers[x];

        var result = (ushort)(_registers[x] >> shiftAmount);

        UpdateLogicFlags(result);
        bool carry = false;
        if (shiftAmount > 0)
            carry = ((original >> (shiftAmount - 1)) & 1) == 1; // did we shift a 1 out of the bottom?
        SetFlag(carry, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        _registers[x] = result;
    }

    private partial void Execute_CMP(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var result = _registers[x] - _registers[y];
        UpdateFlags(result, _registers[x], _registers[y], true);
    }

    private partial void Execute_ADC(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var carry = IsFlagOn(CpuFlags.Carry) ? 1 : 0;
        var result = _registers[x] + _registers[y] + carry;
    }

    private partial void Execute_INC(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);

        var result = _registers[x] + 1;
        UpdateFlags(result, _registers[x], 1);
        _registers[x] = (ushort)result;
    }

    private partial void Execute_DEC(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);

        var result = _registers[x] - 1;
        UpdateFlags(result, _registers[x], 1, true);
        _registers[x] = (ushort)result;
    }

    private partial void Execute_NOT(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);

        var result = (ushort)~_registers[x];

        UpdateLogicFlags(result);
        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        _registers[x] = result;
    }

    private partial void Execute_NEG(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);

        var result = 0 - _registers[x];

        UpdateFlags(result, 0, _registers[x], true);

        _registers[x] = (ushort)result;
    }

    private partial void Execute_IADD(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var x = _memory.ReadByte(_pc + 3);
        ushort oldValue = _memory.ReadWord(addr);
        ushort registerValue = _registers[x];

        var result = oldValue + registerValue;
        UpdateFlags(result, oldValue, registerValue, false);
        _memory.WriteWord(addr, (ushort)result);
    }

    private partial void Execute_ISUB(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var x = _memory.ReadByte(_pc + 3);
        ushort oldValue = _memory.ReadWord(addr);
        ushort registerValue = _registers[x];

        var result = oldValue - registerValue;
        UpdateFlags(result, oldValue, registerValue, true);
        _memory.WriteWord(addr, (ushort)result);
    }

    private partial void Execute_IMUL(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var x = _memory.ReadByte(_pc + 3);
        ushort oldValue = _memory.ReadWord(addr);
        ushort registerValue = _registers[x];

        var result = oldValue * registerValue;
        var truncated = (ushort)result;
        UpdateLogicFlags(truncated);
        SetFlag(result > ushort.MaxValue, CpuFlags.Carry);
        SetFlag(result > ushort.MaxValue, CpuFlags.Overflow);
        _memory.WriteWord(addr, (ushort)result);
    }

    private partial void Execute_IDIV(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var x = _memory.ReadByte(_pc + 3);
        ushort oldValue = _memory.ReadWord(addr);
        ushort registerValue = _registers[x];

        if (registerValue == 0)
        {
            _memory.WriteWord(addr, 0);
            FlagRegister &= 0xFFF0;
            SetFlag(true, CpuFlags.Carry);
            SetFlag(true, CpuFlags.Overflow);
            return;
        }

        var result = (ushort)(oldValue / registerValue);
        UpdateLogicFlags(result);
        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);
        _memory.WriteWord(addr, (ushort)result);
    }

    private partial void Execute_IMOD(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var x = _memory.ReadByte(_pc + 3);
        ushort oldValue = _memory.ReadWord(addr);
        ushort registerValue = _registers[x];

        if (registerValue == 0)
        {
            _memory.WriteWord(addr, 0);
            FlagRegister &= 0xFFF0;
            SetFlag(true, CpuFlags.Carry);
            SetFlag(true, CpuFlags.Overflow);
            return;
        }

        var result = (ushort)(oldValue % registerValue);
        UpdateLogicFlags(result);
        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);
        _memory.WriteWord(addr, (ushort)result);
    }

    private partial void Execute_IAND(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var x = _memory.ReadByte(_pc + 3);
        var result = (ushort)(_memory.ReadWord(addr) & _registers[x]);
        UpdateLogicFlags(result);
        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);
        _memory.WriteWord(addr, result);
    }

    private partial void Execute_IOR(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var x = _memory.ReadByte(_pc + 3);
        var result = (ushort)(_memory.ReadWord(addr) | _registers[x]);
        UpdateLogicFlags(result);
        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);
        _memory.WriteWord(addr, result);
    }

    private partial void Execute_IXOR(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var x = _memory.ReadByte(_pc + 3);
        var result = (ushort)(_memory.ReadWord(addr) ^ _registers[x]);
        UpdateLogicFlags(result);
        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);
        _memory.WriteWord(addr, result);
    }

    private partial void Execute_DINC(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var oldValue = _memory.ReadWord(addr);
        var result = oldValue + 1;
        UpdateFlags(result, oldValue, 1);
        _memory.WriteWord(addr, (ushort)result);
    }

    private partial void Execute_DDEC(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var oldValue = _memory.ReadWord(addr);
        var result = oldValue - 1;
        UpdateFlags(result, oldValue, 1, true);
        _memory.WriteWord(addr, (ushort)result);
    }

    private partial void Execute_DADD(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var oldValue = _memory.ReadWord(addr);
        var immediate = _memory.ReadByte(_pc + 3);
        var result = oldValue + immediate;
        UpdateFlags(result, oldValue, immediate);
        _memory.WriteWord(addr, (ushort)result);
    }

    private partial void Execute_DSUB(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var oldValue = _memory.ReadWord(addr);
        var immediate = _memory.ReadByte(_pc + 3);
        var result = oldValue - immediate;
        UpdateFlags(result, oldValue, immediate, true);
        _memory.WriteWord(addr, (ushort)result);
    }

    private partial void Execute_DMOV(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var imm = _memory.ReadByte(_pc + 3);
        _memory.WriteByte(addr, imm);
    }

    private partial void Execute_DSET(byte opcode, ref ushort pcDelta)
    {
        var addr = _memory.ReadWord(_pc + 1);
        var imm = _memory.ReadWord(_pc + 3);
        _memory.WriteWord(addr, imm);
    }

    private partial void Execute_JMP(byte opcode, ref ushort pcDelta)
    {
        var target = _memory.ReadWord(_pc + 1);
        _pc = target;
        pcDelta = 0;
    }

    private partial void Execute_JEQ(byte opcode, ref ushort pcDelta)
    {
        var target = _memory.ReadWord(_pc + 1);
        if (IsFlagOn(CpuFlags.Zero))
        {
            _pc = target;
            pcDelta = 0;
        }
    }

    private partial void Execute_JNE(byte opcode, ref ushort pcDelta)
    {
        var target = _memory.ReadWord(_pc + 1);
        if (!IsFlagOn(CpuFlags.Zero))
        {
            _pc = target;
            pcDelta = 0;
        }
    }

    private partial void Execute_JGT(byte opcode, ref ushort pcDelta)
    {
        var target = _memory.ReadWord(_pc + 1);
        var zero = IsFlagOn(CpuFlags.Zero);
        var negative = IsFlagOn(CpuFlags.Negative);
        var overflow = IsFlagOn(CpuFlags.Overflow);

        if (!zero && negative == overflow)
        {
            _pc = target;
            pcDelta = 0;
        }
    }

    private partial void Execute_JLT(byte opcode, ref ushort pcDelta)
    {
        var target = _memory.ReadWord(_pc + 1);
        var negative = IsFlagOn(CpuFlags.Negative);
        var overflow = IsFlagOn(CpuFlags.Overflow);

        if (negative != overflow)
        {
            _pc = target;
            pcDelta = 0;
        }
    }

    private partial void Execute_CALL(byte opcode, ref ushort pcDelta)
    {
        var target = _memory.ReadWord(_pc + 1);
        var returnAddress = (ushort)(_pc + 3);
        _sp -= 2;
        _memory.WriteWord(_sp, returnAddress);
        _pc = target;
        pcDelta = 0;
    }

    private partial void Execute_RET(byte opcode, ref ushort pcDelta)
    {
        var returnAddress = _memory.ReadWord(_sp);
        _sp += 2;
        _pc = returnAddress;
        pcDelta = 0;
    }

    private partial void Execute_PUSH(byte opcode, ref ushort pcDelta)
    {
        var x = _memory.ReadByte(_pc + 1);
        _sp -= 2;
        _memory.WriteWord(_sp, _registers[x]);
    }

    private partial void Execute_POP(byte opcode, ref ushort pcDelta)
    {
        var x = _memory.ReadByte(_pc + 1);
        var value = _memory.ReadWord(_sp);
        _sp += 2;
        _registers[x] = value;
    }

    private partial void Execute_DRAW(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();
        var spriteId = _memory.ReadByte(_pc + 2);
        var attributes = (byte)SpriteAttributeRegister;
        var addr = Memory.OamStart + OamRegister;
        OamRegister += 4;
        _memory.WriteByte(addr, (byte)_registers[x]);
        _memory.WriteByte(addr + 1, (byte)_registers[y]);
        _memory.WriteByte(addr + 2, spriteId);
        _memory.WriteByte(addr + 3, attributes);
    }

    private partial void Execute_CLS(byte opcode, ref ushort pcDelta)
    {
        var packed = (byte)((_memory.ReadByte(_pc + 1) << 4) | (_memory.ReadByte(_pc + 1) & 0x0F));
        _mobo.ClearScreen(packed);
    }

    private partial void Execute_VBLNK(byte opcode, ref ushort pcDelta)
    {
        _mobo.AwaitVBlank();
    }

    private partial void Execute_PLAY(byte opcode, ref ushort pcDelta)
    {
        var channel = _memory.ReadByte(_pc + 1);
        var note = _memory.ReadByte(_pc + 2);
        var instrument = _memory.ReadByte(_pc + 3);
        if (channel > 7)
            channel = 7;
        _mobo.PlayNote(channel, note, instrument);
    }

    private partial void Execute_STOP(byte opcode, ref ushort pcDelta)
    {
        var rChannel = _memory.ReadByte(_pc + 1);
        var channel = (byte)_registers[rChannel];
        if (channel > 7)
            channel = (byte)7;
        _mobo.StopChannel(channel);
    }

    private partial void Execute_INPUT(byte opcode, ref ushort pcDelta)
    {
        var packed = _memory.ReadByte(_pc + 1);
        var x = packed >> 4;
        var controller = (byte)((packed & 3) >> 1); // AND by 3 (00000011) and shift right by 1, if the last bit is 0 we have the first controller and if it is 1 we have the second.
        _registers[x] = _mobo.GetInputState(controller);
    }

    private partial void Execute_RND(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);
        var max = _memory.ReadWord(_pc + 1);
        _registers[x] = (ushort)_rng.Next(max);
    }

    private partial void Execute_TEXT(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();
        var charCode = _memory.ReadByte(_pc + 2);
        Console.WriteLine($"Executing TEXT with: {x}, {y}, {charCode}");
        _mobo.DrawChar(_registers[x], _registers[y], charCode);
    }

    private partial void Execute_ATTR(byte opcode, ref ushort pcDelta)
    {
        var attributes = _memory.ReadByte(_pc + 1);
        _mobo.SetTextAttributes(attributes);
    }

    private partial void Execute_SWC(byte opcode, ref ushort pcDelta)
    {
        var oldIndex = (byte)(_memory.ReadByte(_pc + 1) & 0x0F); // truncate to avoid tomfoolery
        var newIndex = (byte)(_memory.ReadByte(_pc + 2) & 0x1F);
        _mobo.SwapColor(oldIndex, newIndex);
    }

    private partial void Execute_FLPH(byte opcode, ref ushort pcDelta)
    {
        var value = _memory.ReadByte(_pc + 1);
        if (value != 0)
            SpriteAttributeRegister |= 0x01; // true
        else
            SpriteAttributeRegister &= 0xFE; // false
    }

    private partial void Execute_FLPV(byte opcode, ref ushort pcDelta)
    {
        var value = _memory.ReadByte(_pc + 1);
        if (value != 0)
            SpriteAttributeRegister |= 0x02; // true
        else
            SpriteAttributeRegister &= 0xFD; // false
    }

    private partial void Execute_SONG(byte opcode, ref ushort pcDelta)
    {
        Console.WriteLine("SONG");
        var x = IndexFromOpcode(opcode);
        var songAddr = _registers[x];
        _mobo.StartSequencer(songAddr);
    }

    private partial void Execute_MUTE(byte opcode, ref ushort pcDelta)
    {
        _mobo.StopAllSounds();
    }
}
