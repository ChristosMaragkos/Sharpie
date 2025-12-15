using System;

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
}