namespace Sharpie.Core;

public partial class Cpu
{
    private partial void Execute_PREFIX(byte opcode, ref ushort pcDelta)
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
                _registers[x] = _memory.ReadWord(address);
                break;
            }

            case >= 0x20
            and <= 0x2F: // LDI
            {
                pcDelta = 3;
                var x = IndexFromOpcode(prefixed);
                var data = _memory.ReadByte(_pc + 1);
                _registers[x] = (ushort)data;
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
                _memory.ClearRange(Memory.OamStart, Memory.WorkRamStart - Memory.OamStart);
                OamRegister = 0;
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
