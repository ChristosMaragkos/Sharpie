namespace Sharpie.Core.Hardware;

public partial class Cpu
{
    private partial void Execute_ALT(byte opcode, ref ushort pcDelta)
    {
        var prefixed = _memory.ReadByte(_pc + 1);
        _pc++; // necessary to read opcode args correctly

        switch (prefixed)
        {
            case >= 0x10 and <= 0x1F: // LDM
            {
                pcDelta = 3;
                var x = IndexFromOpcode(prefixed);
                var address = _memory.ReadWord(_pc + 1);
                _registers[x] = _memory.ReadByte(address);
                break;
            }

            case >= 0x30
            and <= 0x3F: // STM
            {
                pcDelta = 3;
                var x = IndexFromOpcode(prefixed);
                var lowByte = (byte)((_registers[x] & 0x00FF));
                var address = _memory.ReadWord(_pc + 1);
                _memory.WriteByte(address, lowByte);
                break;
            }

            case 0xF1: // CLS
            {
                pcDelta = 2;
                Execute_CLS(prefixed, ref pcDelta);
                _memory.FillRange(Memory.OamStart, Memory.WorkRamStart - Memory.OamStart, 0xFF);
                OamRegister = 0;
                break;
            }

            case 0xC0: // SETCRS
            {
                pcDelta = 3;
                var xDelta = (sbyte)_memory.ReadByte(_pc + 1);
                var yDelta = (sbyte)_memory.ReadByte(_pc + 2);
                CursorPosX += xDelta;
                CursorPosY += yDelta;
                break;
            }

            case >= 0xD0
            and <= 0xDF: // DRAW
            {
                pcDelta = 3;
                var rOamSlot = IndexFromOpcode(prefixed);
                var (xReg, yReg) = ReadRegisterArgs();
                var (sprIdReg, attrReg) = ReadRegisterArgs(2);

                var oamSlot = _registers[rOamSlot] % (MaxOamSlots / 4);
                var (x, y) = (_registers[xReg], _registers[yReg]);
                var (sprId, attr) = (_registers[sprIdReg], _registers[attrReg]);

                if ((oamSlot * 4) == OamRegister)
                    OamRegister += 4;
                var addr = Memory.OamStart + (oamSlot * 4);
                _memory.WriteByte(addr, (byte)x);
                _memory.WriteByte(addr + 1, (byte)y);
                _memory.WriteByte(addr + 2, (byte)sprId);
                _memory.WriteByte(addr + 3, (byte)attr);
                break;
            }

            case 0x60: // IADD
            {
                pcDelta = 3;
                var x = _memory.ReadByte(_pc + 1);
                var imm = _memory.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _memory.ReadWord(ptr);
                var result = old + imm;
                UpdateFlags(result, old, imm);
                _memory.WriteWord(ptr, (ushort)result);
                break;
            }

            case 0x61: // ISUB
            {
                pcDelta = 3;
                var x = _memory.ReadByte(_pc + 1);
                var imm = _memory.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _memory.ReadWord(ptr);
                var result = old - imm;
                UpdateFlags(result, old, imm, true);
                _memory.WriteWord(ptr, (ushort)result);
                break;
            }

            case 0x62: // IMUL
            {
                pcDelta = 3;
                var x = _memory.ReadByte(_pc + 1);
                var imm = _memory.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _memory.ReadWord(ptr);
                var result = (ushort)(old * imm);
                UpdateLogicFlags(result);
                _memory.WriteWord(ptr, result);
                break;
            }

            case 0x63: // IDIV
            {
                pcDelta = 3;
                var x = _memory.ReadByte(_pc + 1);
                var imm = _memory.ReadByte(_pc + 2);
                var ptr = _registers[x];

                if (imm == 0)
                {
                    FlagRegister &= 0xFFF0;
                    SetFlag(true, CpuFlags.Zero);
                    SetFlag(true, CpuFlags.Overflow);
                    _memory.WriteWord(ptr, 0);
                }

                var old = _memory.ReadWord(ptr);
                var result = (ushort)(old / imm);
                UpdateLogicFlags(result);
                _memory.WriteWord(ptr, result);
                break;
            }

            case 0x64: // IMOD
            {
                pcDelta = 3;
                var x = _memory.ReadByte(_pc + 1);
                var imm = _memory.ReadByte(_pc + 2);
                var ptr = _registers[x];

                if (imm == 0)
                {
                    FlagRegister &= 0xFFF0;
                    SetFlag(true, CpuFlags.Zero);
                    SetFlag(true, CpuFlags.Overflow);
                    _memory.WriteWord(ptr, 0);
                }

                var old = _memory.ReadWord(ptr);
                var result = (ushort)(old % imm);
                UpdateLogicFlags(result);
                _memory.WriteWord(ptr, result);
                break;
            }

            case 0x65: // IAND
            {
                pcDelta = 3;
                var x = _memory.ReadByte(_pc + 1);
                var imm = _memory.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _memory.ReadWord(ptr);
                var result = (ushort)(old & imm);
                UpdateLogicFlags(result);
                SetFlag(false, CpuFlags.Overflow);
                SetFlag(false, CpuFlags.Carry);
                _memory.WriteWord(ptr, result);
                break;
            }

            case 0x66: // IOR
            {
                pcDelta = 3;
                var x = _memory.ReadByte(_pc + 1);
                var imm = _memory.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _memory.ReadWord(ptr);
                var result = (ushort)(old | imm);
                UpdateLogicFlags(result);
                SetFlag(false, CpuFlags.Overflow);
                SetFlag(false, CpuFlags.Carry);
                _memory.WriteWord(ptr, result);
                break;
            }

            case 0x67: // IXOR
            {
                pcDelta = 3;
                var x = _memory.ReadByte(_pc + 1);
                var imm = _memory.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _memory.ReadWord(ptr);
                var result = (ushort)(old ^ imm);
                UpdateLogicFlags(result);
                SetFlag(false, CpuFlags.Overflow);
                SetFlag(false, CpuFlags.Carry);
                _memory.WriteWord(ptr, result);
                break;
            }

            case 0x68: // ICMP
            {
                pcDelta = 3;
                var x = _memory.ReadByte(_pc + 1);
                var imm = _memory.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _memory.ReadWord(ptr);
                var result = old - imm;
                UpdateFlags(result, old, imm, true);
                break;
            }

            default:
                Console.WriteLine($"Unknown Opcode: 0x{opcode:X2}");
                IsHalted = true;
                pcDelta = 1;
                break;
        }
    }
}
