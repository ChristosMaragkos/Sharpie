namespace Sharpie.Core.Hardware;

internal partial class Cpu
{
    private partial void Execute_MOV(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        GetRegister(x) = GetRegister(y);
    }

    private partial void Execute_LDM(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var address = _mobo.ReadWord((_pc + 2));
        GetRegister(x) = _mobo.ReadWord(address);
    }

    private partial void Execute_LDP(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();
        var address = GetRegister(y);
        GetRegister(x) = _mobo.ReadWord(address);
    }

    private partial void Execute_LDI(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);
        var value = _mobo.ReadWord(_pc + 1);
        GetRegister(x) = value;
    }

    private partial void Execute_STM(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);
        var address = _mobo.ReadWord(_pc + 1);
        _mobo.WriteWord(address, GetRegister(x));
    }

    private partial void Execute_ADD(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var result = GetRegister(x) + GetRegister(y);
        UpdateFlags(result, GetRegister(x), GetRegister(y));
        GetRegister(x) = (ushort)result;
    }

    private partial void Execute_SUB(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var result = (ushort)(GetRegister(x) - GetRegister(y));
        UpdateFlags(result, GetRegister(x), GetRegister(y), true);
        GetRegister(x) = (ushort)result;
    }

    private partial void Execute_MUL(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        int result = GetRegister(x) * GetRegister(y);
        var truncated = (ushort)result;

        UpdateLogicFlags(truncated);
        var dataLost = result > ushort.MaxValue;
        SetFlag(dataLost, CpuFlags.Carry);
        SetFlag(dataLost, CpuFlags.Overflow);
    }

    private partial void Execute_DIV(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();
        ushort valY = GetRegister(y);

        if (valY == 0)
        {
            GetRegister(x) = 0;

            FlagRegister &= 0xFFF0;
            SetFlag(true, CpuFlags.Zero);
            SetFlag(true, CpuFlags.Overflow);
            return;
        }

        var result = (ushort)(GetRegister(x) / valY);

        UpdateLogicFlags(result);

        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        GetRegister(x) = result;
    }

    private partial void Execute_MOD(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();
        ushort valY = GetRegister(y);

        if (valY == 0)
        {
            GetRegister(x) = 0;

            FlagRegister &= 0xFFF0;
            SetFlag(true, CpuFlags.Zero);
            SetFlag(true, CpuFlags.Overflow);
            return;
        }

        var result = (ushort)(GetRegister(x) % valY);
        UpdateLogicFlags(result);

        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        GetRegister(x) = result;
    }

    private partial void Execute_AND(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var result = (ushort)(GetRegister(x) & GetRegister(y));
        UpdateLogicFlags(result);

        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        GetRegister(x) = result;
    }

    private partial void Execute_OR(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var result = (ushort)(GetRegister(x) | GetRegister(y));
        UpdateLogicFlags(result);

        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        GetRegister(x) = result;
    }

    private partial void Execute_XOR(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var result = (ushort)(GetRegister(x) ^ GetRegister(y));
        UpdateLogicFlags(result);

        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        GetRegister(x) = result;
    }

    private partial void Execute_SHL(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var shiftAmount = GetRegister(y) & 0x0F;
        var original = GetRegister(x);

        var result = GetRegister(x) << shiftAmount;
        var truncated = (ushort)result;

        UpdateLogicFlags(truncated);
        SetFlag(result > ushort.MaxValue, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        GetRegister(x) = truncated;
    }

    private partial void Execute_SHR(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var shiftAmount = GetRegister(y) & 0x0F;
        var original = GetRegister(x);

        var result = (ushort)(GetRegister(x) >> shiftAmount);

        UpdateLogicFlags(result);
        bool carry = false;
        if (shiftAmount > 0)
            carry = ((original >> (shiftAmount - 1)) & 1) == 1; // did we shift a 1 out of the bottom?
        SetFlag(carry, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        GetRegister(x) = result;
    }

    private partial void Execute_CMP(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var result = GetRegister(x) - GetRegister(y);
        UpdateFlags(result, GetRegister(x), GetRegister(y), true);
    }

    private partial void Execute_ADC(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();

        var carry = IsFlagOn(CpuFlags.Carry) ? 1 : 0;
        var result = GetRegister(x) + GetRegister(y) + carry;
    }

    private partial void Execute_INC(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var result = (ushort)(GetRegister(x) + 1);
        UpdateFlags(result, GetRegister(x), 1);
        GetRegister(x) = result;
    }

    private partial void Execute_DEC(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var result = GetRegister(x) - 1;
        UpdateFlags(result, GetRegister(x), 1, true);
        GetRegister(x) = (ushort)result;
    }

    private partial void Execute_NOT(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var result = (ushort)~GetRegister(x);
        UpdateLogicFlags(result);
        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);

        GetRegister(x) = result;
    }

    private partial void Execute_NEG(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);

        var result = 0 - GetRegister(x);

        UpdateFlags(result, 0, GetRegister(x), true);

        GetRegister(x) = (ushort)result;
    }

    private partial void Execute_IADD(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var imm = _mobo.ReadByte(_pc + 2);

        var result = GetRegister(x) + imm;
        UpdateFlags(result, GetRegister(x), imm);
        GetRegister(x) = (ushort)result;
    }

    private partial void Execute_ISUB(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var imm = _mobo.ReadByte(_pc + 2);

        var result = GetRegister(x) - imm;
        UpdateFlags(result, GetRegister(x), imm, true);
        GetRegister(x) = (ushort)result;
    }

    private partial void Execute_IMUL(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var imm = _mobo.ReadByte(_pc + 2);

        var result = (ushort)(GetRegister(x) * imm);
        UpdateLogicFlags(result);
        GetRegister(x) = result;
    }

    private partial void Execute_IDIV(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var imm = _mobo.ReadByte(_pc + 2);

        if (imm == 0)
        {
            FlagRegister &= 0xFFF0;
            GetRegister(x) = 0;
            SetFlag(true, CpuFlags.Zero);
            SetFlag(true, CpuFlags.Overflow);
            return;
        }

        var result = (ushort)(GetRegister(x) / imm);
        UpdateLogicFlags(result);
        GetRegister(x) = result;
    }

    private partial void Execute_IMOD(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var imm = _mobo.ReadByte(_pc + 2);

        if (imm == 0)
        {
            FlagRegister &= 0xFFF0;
            GetRegister(x) = 0;
            SetFlag(true, CpuFlags.Zero);
            SetFlag(true, CpuFlags.Overflow);
            return;
        }

        var result = (ushort)(GetRegister(x) % imm);
        UpdateLogicFlags(result);
        GetRegister(x) = result;
    }

    private partial void Execute_IAND(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var imm = _mobo.ReadByte(_pc + 2);

        var result = (ushort)(GetRegister(x) & imm);
        UpdateLogicFlags(result);
        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);
        GetRegister(x) = result;
    }

    private partial void Execute_IOR(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var imm = _mobo.ReadByte(_pc + 2);

        var result = (ushort)(GetRegister(x) | imm);
        UpdateLogicFlags(result);
        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);
        GetRegister(x) = result;
    }

    private partial void Execute_IXOR(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var imm = _mobo.ReadByte(_pc + 2);

        var result = (ushort)(GetRegister(x) ^ imm);
        UpdateLogicFlags(result);
        SetFlag(false, CpuFlags.Carry);
        SetFlag(false, CpuFlags.Overflow);
        GetRegister(x) = result;
    }

    private partial void Execute_ICMP(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var imm = _mobo.ReadByte(_pc + 2);

        var result = GetRegister(x) - imm;
        UpdateFlags(result, GetRegister(x), imm, true);
    }

    private partial void Execute_DINC(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var word = _mobo.ReadWord(GetRegister(x));

        var result = word + 1;
        UpdateFlags(result, GetRegister(x), 1);
        _mobo.WriteWord(GetRegister(x), (ushort)result);
    }

    private partial void Execute_DDEC(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var word = _mobo.ReadWord(GetRegister(x));

        var result = word - 1;
        UpdateFlags(result, GetRegister(x), 1, true);
        _mobo.WriteWord(GetRegister(x), (ushort)result);
    }

    private partial void Execute_JMP(byte opcode, ref ushort pcDelta)
    {
        var target = _mobo.ReadWord(_pc + 1);
        _pc = target;
        pcDelta = 0;
    }

    private partial void Execute_JEQ(byte opcode, ref ushort pcDelta)
    {
        var target = _mobo.ReadWord(_pc + 1);
        if (IsFlagOn(CpuFlags.Zero))
        {
            _pc = target;
            pcDelta = 0;
        }
    }

    private partial void Execute_JNE(byte opcode, ref ushort pcDelta)
    {
        var target = _mobo.ReadWord(_pc + 1);
        if (!IsFlagOn(CpuFlags.Zero))
        {
            _pc = target;
            pcDelta = 0;
        }
    }

    private partial void Execute_JGT(byte opcode, ref ushort pcDelta)
    {
        var target = _mobo.ReadWord(_pc + 1);
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
        var target = _mobo.ReadWord(_pc + 1);
        var negative = IsFlagOn(CpuFlags.Negative);
        var overflow = IsFlagOn(CpuFlags.Overflow);

        if (negative != overflow)
        {
            _pc = target;
            pcDelta = 0;
        }
    }

    private partial void Execute_JGE(byte opcode, ref ushort pcDelta)
    {
        var target = _mobo.ReadWord(_pc + 1);
        var negative = IsFlagOn(CpuFlags.Negative);
        var overflow = IsFlagOn(CpuFlags.Overflow);

        if (negative == overflow)
        {
            _pc = target;
            pcDelta = 0;
        }
    }

    private partial void Execute_JLE(byte opcode, ref ushort pcDelta)
    {
        var target = _mobo.ReadWord(_pc + 1);
        var zero = IsFlagOn(CpuFlags.Zero);
        var negative = IsFlagOn(CpuFlags.Negative);
        var overflow = IsFlagOn(CpuFlags.Overflow);

        if (zero && negative != overflow)
        {
            _pc = target;
            pcDelta = 0;
        }
    }

    private partial void Execute_CALL(byte opcode, ref ushort pcDelta)
    {
        var target = _mobo.ReadWord(_pc + 1);
        var returnAddress = (ushort)(_pc + 3);
        _callStack.Push(returnAddress);
        _pc = target;
        pcDelta = 0;
    }

    private partial void Execute_RET(byte opcode, ref ushort pcDelta)
    {
        var returnAddress = _callStack.Pop();
        _pc = returnAddress;
        pcDelta = 0;
    }

    private partial void Execute_PUSH(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var addr = GetRegister(x);
        _callStack.Push(addr);
    }

    private partial void Execute_POP(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        var value = _callStack.Pop();
        GetRegister(x) = value;
    }

    private partial void Execute_DRAW(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);
        var (y, sprIdReg) = ReadRegisterArgs();
        var attrReg = _mobo.ReadByte(_pc + 2);
        var spriteId = (byte)GetRegister(sprIdReg);
        var attrType = GetRegister(attrReg);

        var attr = (byte)attrType; // get the low byte
        var type = (byte)(attrType >> 8);

        var slotIndex = _mobo.GetOamCursor();
        _tagMap[slotIndex] = attr;
        _mobo.WriteSpriteEntry(GetRegister(x), GetRegister(y), spriteId, attr, type);
    }

    private partial void Execute_CLS(byte opcode, ref ushort pcDelta)
    {
        var idx = _mobo.ReadByte(_pc + 1) & 0x0F;
        var color = (byte)(GetRegister(idx) & 0xF);
        _mobo.ClearScreen(color);
    }

    private partial void Execute_VBLNK(byte opcode, ref ushort pcDelta)
    {
        AwaitVBlank();
    }

    private partial void Execute_PLAY(byte opcode, ref ushort pcDelta)
    {
        var (channelReg, noteReg) = ReadRegisterArgs();
        var instrumentReg = _mobo.ReadByte(_pc + 2) & 0x0F;

        var channel = (byte)GetRegister(channelReg);
        var note = (byte)GetRegister(noteReg);
        var instrument = (byte)GetRegister(instrumentReg);
        if (channel > 7)
            channel = 7;
        _mobo.PlayNote(channel, note, instrument);
    }

    private partial void Execute_STOP(byte opcode, ref ushort pcDelta)
    {
        var rChannel = _mobo.ReadByte(_pc + 1);
        var channel = (byte)GetRegister(rChannel);
        if (channel > 7)
            channel = (byte)7;
        _mobo.StopChannel(channel);
    }

    private partial void Execute_INPUT(byte opcode, ref ushort pcDelta)
    {
        var (rController, rDest) = ReadRegisterArgs();
        GetRegister(rDest) = _mobo.ControllerStates[GetRegister(rController) & 1];
    }

    private partial void Execute_RND(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);
        var max = _mobo.ReadWord(_pc + 1);
        GetRegister(x) = (ushort)_rng.Next(max);
    }

    private partial void Execute_TEXT(byte opcode, ref ushort pcDelta)
    {
        var x = CursorPosX;
        var y = CursorPosY;
        var charCode = _mobo.ReadByte(_pc + 1);
        _mobo.DrawChar(x, y, charCode);

        CursorPosX++;
    }

    private partial void Execute_ATTR(byte opcode, ref ushort pcDelta)
    {
        var attributes = _mobo.ReadByte(_pc + 1);
        _mobo.SetTextAttributes(attributes);
    }

    private partial void Execute_SWC(byte opcode, ref ushort pcDelta)
    {
        var (oldIndex, newIndex) = ReadRegisterArgs();
        _mobo.SwapColor((byte)(GetRegister(oldIndex) & 0x0F), (byte)(GetRegister(newIndex) & 0x1F));
    }

    private partial void Execute_SONG(byte opcode, ref ushort pcDelta)
    {
        var x = IndexFromOpcode(opcode);
        var songAddr = GetRegister(x);
        _mobo.StartSequencer(songAddr);
    }

    private partial void Execute_MUTE(byte opcode, ref ushort pcDelta)
    {
        _mobo.ToggleSequencer();
        _mobo.StopAllSounds();
    }

    private partial void Execute_FLIPR(byte opcode, ref ushort pcDelta)
    {
        FlipRegisterBanks();
    }

    private partial void Execute_CAM(byte opcode, ref ushort pcDelta)
    {
        var (x, y) = ReadRegisterArgs();
        _mobo.MoveCamera((short)GetRegister(x), (short)GetRegister(y));
    }

    private partial void Execute_GETOAM(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        GetRegister(x) = (ushort)_mobo.GetOamCursor();
    }

    private partial void Execute_SETOAM(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        _mobo.SetOamCursor(GetRegister(x)); // TODO: Make sure to throw a beautiful little segfault when we set to an index above the max
    }

    private partial void Execute_COL(byte opcode, ref ushort pcDelta)
    {
        var (rSource, rDest) = ReadRegisterArgs();
        GetRegister(rDest) = _mobo.CheckCollision(GetRegister(rSource));
    }

    private partial void Execute_TAG(byte opcode, ref ushort pcDelta)
    {
        var (rSource, rDest) = ReadRegisterArgs();
        GetRegister(rDest) = _tagMap[rSource];
    }

    private partial void Execute_SETCRS(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1) & 0x1F;
        var y = _mobo.ReadByte(_pc + 2) & 0x1F;

        CursorPosX = x;
        CursorPosY = y;
    }

    private partial void Execute_INSTR(byte opcode, ref ushort pcDelta)
    {
        var instIdReg = IndexFromOpcode(opcode);
        var instId = GetRegister(instIdReg);
        var (a, d) = ReadRegisterArgs(); // not register args but still 4-bit values
        var (s, r) = ReadRegisterArgs(2);

        var attack = (byte)(a * 17);
        var decay = (byte)(d * 17);
        var sustain = (byte)(s * 17);
        var release = (byte)(r * 17);

        var addr = Memory.AudioRamStart + 32 + (instId * 4);
        _mobo.WriteByte(addr, attack);
        _mobo.WriteByte(addr + 1, decay);
        _mobo.WriteByte(addr + 2, sustain);
        _mobo.WriteByte(addr + 3, release);
    }

    private partial void Execute_OUT_R(byte opcode, ref ushort pcDelta)
    {
        var x = _mobo.ReadByte(_pc + 1);
        _mobo.PushDebug(
            $"Register {x + _registerBankOffset} @ address ${_pc:X4}: {GetRegister(x)}"
        );
    }

    private partial void Execute_OUT_B(byte opcode, ref ushort pcDelta)
    {
        var b = _mobo.ReadByte(_pc + 1);
        _mobo.PushDebug($"8-bit value @ address ${_pc:X4}: {b}");
    }

    private partial void Execute_OUT_W(byte opcode, ref ushort pcDelta)
    {
        var w = _mobo.ReadWord(_pc + 1);
        _mobo.PushDebug($"16-bit value @ address ${_pc:X4}: {w}");
    }
}
