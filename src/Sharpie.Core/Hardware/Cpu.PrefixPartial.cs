namespace Sharpie.Core.Hardware;

internal partial class Cpu
{
    private partial void Execute_ALT(byte opcode, ref ushort pcDelta)
    {
        var prefixed = _mobo.ReadByte(_pc + 1);
        _pc++; // necessary to read opcode args correctly

        switch (prefixed)
        {
            case 0x10: // LDM
            {
                pcDelta = 4;
                var x = _mobo.ReadByte(_pc + 1);
                var address = _mobo.ReadWord(_pc + 2);
                _registers[x] = _mobo.ReadByte(address);
                break;
            }

            case 0x11: // LDP
            {
                pcDelta = 2;
                var (x, y) = ReadRegisterArgs();
                var address = _registers[y];
                _registers[x] = _mobo.ReadByte(address);
                break;
            }

            case >= 0x30
            and <= 0x3F: // STM
            {
                pcDelta = 3;
                var x = IndexFromOpcode(prefixed);
                var lowByte = (byte)((_registers[x] & 0x00FF));
                var address = _mobo.ReadWord(_pc + 1);
                _mobo.WriteByte(address, lowByte);
                break;
            }

            case 0xF1: // CLS
            {
                pcDelta = 2;
                Execute_CLS(prefixed, ref pcDelta);
                _mobo.FillRange(Memory.OamStart, Memory.WorkRamStart - Memory.OamStart, 0xFF);
                OamRegister = 0;
                break;
            }

            case 0xC0: // SETCRS
            {
                pcDelta = 3;
                var xDelta = (sbyte)_mobo.ReadByte(_pc + 1);
                var yDelta = (sbyte)_mobo.ReadByte(_pc + 2);
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

                var oamSlot = _registers[rOamSlot] % (2048 / 4);
                var (x, y) = (_registers[xReg], _registers[yReg]);
                var (sprId, attr) = (_registers[sprIdReg], _registers[attrReg]);

                if ((oamSlot * 4) == OamRegister)
                    OamRegister += 4;
                var addr = Memory.OamStart + (oamSlot * 4);
                _mobo.WriteByte(addr, (byte)x);
                _mobo.WriteByte(addr + 1, (byte)y);
                _mobo.WriteByte(addr + 2, (byte)sprId);
                _mobo.WriteByte(addr + 3, (byte)attr);
                break;
            }

            case 0x60: // IADD
            {
                pcDelta = 3;
                var x = _mobo.ReadByte(_pc + 1);
                var imm = _mobo.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _mobo.ReadWord(ptr);
                var result = old + imm;
                UpdateFlags(result, old, imm);
                _mobo.WriteWord(ptr, (ushort)result);
                break;
            }

            case 0x61: // ISUB
            {
                pcDelta = 3;
                var x = _mobo.ReadByte(_pc + 1);
                var imm = _mobo.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _mobo.ReadWord(ptr);
                var result = old - imm;
                UpdateFlags(result, old, imm, true);
                _mobo.WriteWord(ptr, (ushort)result);
                break;
            }

            case 0x62: // IMUL
            {
                pcDelta = 3;
                var x = _mobo.ReadByte(_pc + 1);
                var imm = _mobo.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _mobo.ReadWord(ptr);
                var result = (ushort)(old * imm);
                UpdateLogicFlags(result);
                _mobo.WriteWord(ptr, result);
                break;
            }

            case 0x63: // IDIV
            {
                pcDelta = 3;
                var x = _mobo.ReadByte(_pc + 1);
                var imm = _mobo.ReadByte(_pc + 2);
                var ptr = _registers[x];

                if (imm == 0)
                {
                    FlagRegister &= 0xFFF0;
                    SetFlag(true, CpuFlags.Zero);
                    SetFlag(true, CpuFlags.Overflow);
                    _mobo.WriteWord(ptr, 0);
                }

                var old = _mobo.ReadWord(ptr);
                var result = (ushort)(old / imm);
                UpdateLogicFlags(result);
                _mobo.WriteWord(ptr, result);
                break;
            }

            case 0x64: // IMOD
            {
                pcDelta = 3;
                var x = _mobo.ReadByte(_pc + 1);
                var imm = _mobo.ReadByte(_pc + 2);
                var ptr = _registers[x];

                if (imm == 0)
                {
                    FlagRegister &= 0xFFF0;
                    SetFlag(true, CpuFlags.Zero);
                    SetFlag(true, CpuFlags.Overflow);
                    _mobo.WriteWord(ptr, 0);
                }

                var old = _mobo.ReadWord(ptr);
                var result = (ushort)(old % imm);
                UpdateLogicFlags(result);
                _mobo.WriteWord(ptr, result);
                break;
            }

            case 0x65: // IAND
            {
                pcDelta = 3;
                var x = _mobo.ReadByte(_pc + 1);
                var imm = _mobo.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _mobo.ReadWord(ptr);
                var result = (ushort)(old & imm);
                UpdateLogicFlags(result);
                SetFlag(false, CpuFlags.Overflow);
                SetFlag(false, CpuFlags.Carry);
                _mobo.WriteWord(ptr, result);
                break;
            }

            case 0x66: // IOR
            {
                pcDelta = 3;
                var x = _mobo.ReadByte(_pc + 1);
                var imm = _mobo.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _mobo.ReadWord(ptr);
                var result = (ushort)(old | imm);
                UpdateLogicFlags(result);
                SetFlag(false, CpuFlags.Overflow);
                SetFlag(false, CpuFlags.Carry);
                _mobo.WriteWord(ptr, result);
                break;
            }

            case 0x67: // IXOR
            {
                pcDelta = 3;
                var x = _mobo.ReadByte(_pc + 1);
                var imm = _mobo.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _mobo.ReadWord(ptr);
                var result = (ushort)(old ^ imm);
                UpdateLogicFlags(result);
                SetFlag(false, CpuFlags.Overflow);
                SetFlag(false, CpuFlags.Carry);
                _mobo.WriteWord(ptr, result);
                break;
            }

            case 0x68: // ICMP
            {
                pcDelta = 3;
                var x = _mobo.ReadByte(_pc + 1);
                var imm = _mobo.ReadByte(_pc + 2);
                var ptr = _registers[x];

                var old = _mobo.ReadWord(ptr);
                var result = old - imm;
                UpdateFlags(result, old, imm, true);
                break;
            }

            case 0xF7: // TEXT
            {
                pcDelta = 2;
                var x = _mobo.ReadByte(_pc + 1) & 0x0F; // truncate to register index since we tokenize it as a byte
                Console.WriteLine($"X: {x}");
                var digits = _registers[x].ToString();
                Console.WriteLine($"Value: {digits}");
                const int FontZeroIndex = 26 - '0'; // this is the first number's index in the Sharpie font
                foreach (var c in digits)
                {
                    var fontIndex = (byte)(c + FontZeroIndex);
                    Console.WriteLine($"Char: {c}");
                    Console.WriteLine($"Char index: {fontIndex}");
                    _mobo.DrawChar(CursorPosX, CursorPosY, fontIndex);
                    CursorPosX++;
                }

                break;
            }

            case 0xFC: // MUTE
            {
                _mobo.StopAllSounds();
                break;
            }

            default:
                _mobo.PushDebug($"Unknown ALT opcode: {prefixed}");
                IsHalted = true;
                pcDelta = 1;
                break;
        }
    }
}
