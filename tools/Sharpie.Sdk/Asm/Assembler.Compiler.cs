namespace Sharpie.Sdk.Asm;

public partial class Assembler
{
    public readonly byte[] Rom = new byte[Meta.Constants.MaxRomSize];
    private readonly bool[] TouchedBytes = new bool[Meta.Constants.MaxRomSize];

    public void Compile()
    {
        Console.WriteLine("Assembler: Compiling file...");
        CurrentAddress = 0;
        int lineNum = 0;
        foreach (var token in Tokens)
        {
            if (token.Opcode == ".SPRITE" || token.Opcode == ".DEF" || token.Opcode == ".ORG")
                continue;
            lineNum = token.SourceLine!.Value;
            CurrentAddress = token.Address!.Value;

            if (token.Args == null)
                throw new AssemblySyntaxException(
                    $"Expected arguments for opcode {token.Opcode}",
                    lineNum
                );

            for (int i = 0; i < token.Args.Length; i++)
                token.Args[i] = token.Args[i].Trim(' ', ',');

            if (token.Opcode!.StartsWith('.'))
            {
                switch (token.Opcode.ToUpper())
                {
                    case ".ORG":
                        CurrentAddress = ParseWord(token.Args[0], lineNum);
                        break;

                    case ".SPRITE":
                        var spriteIndex = ParseByte(token.Args[0], lineNum);
                        var target = CalculateSpriteAddress(spriteIndex);

                        if (target >= Rom.Length)
                        {
                            throw new SharpieRomSizeException(
                                $"Sprite #{spriteIndex} is out of bounds (Addr: {target})"
                            );
                        }

                        CurrentAddress = target;
                        break;

                    case ".DB":
                    case ".BYTES":
                    case ".DATA":
                        for (int i = 0; i < token.Args.Length; i++)
                        {
                            WriteToRom(ParseByte(token.Args[i], lineNum), i);
                        }
                        break;

                    case ".DW":
                    case ".WORDS":
                        for (int i = 0; i < token.Args.Length; i += 2)
                        {
                            WriteToRom(ParseWord(token.Args[i], lineNum), i);
                        }
                        break;
                }
                continue;
            }

            var pattern = InstructionSet.GetOpcodePattern(token.Opcode);
            var opHex = InstructionSet.GetOpcodeHex(token.Opcode);
            var length = InstructionSet.GetOpcodeLength(token.Opcode);
            var isFam = InstructionSet.IsOpcodeFamily(token.Opcode);
            var argIndex = 0;

            var localOffset = 0;

            if (isFam)
            {
                var arg = token.Args[argIndex];
                argIndex++;
                int regIdx = ParseRegister(arg, lineNum);
                WriteToRom((byte)(opHex | regIdx), localOffset);
                localOffset++;
            }
            else
            {
                WriteToRom((byte)opHex, localOffset++);
            }

            for (int p = isFam ? 1 : 0; p < pattern.Length; p++)
            {
                var cmd = pattern[p];

                switch (cmd)
                {
                    case 'R':
                        if (p + 1 < pattern.Length && pattern[p + 1] == 'R')
                        {
                            byte rA = ParseRegister(token.Args[argIndex++], lineNum);
                            byte rB = ParseRegister(token.Args[argIndex++], lineNum);
                            WriteToRom((byte)(rA << 4 | rB), localOffset++);
                            p++;
                        }
                        else
                        {
                            WriteToRom(
                                ParseRegister(token.Args[argIndex++], lineNum),
                                localOffset++
                            );
                        }
                        break;

                    case 'W':
                        WriteToRom(ParseWord(token.Args[argIndex++], lineNum), localOffset);
                        localOffset += 2;
                        break;
                    case 'B':
                        WriteToRom(ParseByte(token.Args[argIndex++], lineNum), localOffset++);
                        break;
                    default:
                        throw new AssemblySyntaxException(
                            $"The SDK definition of the {token.Opcode} instruction contains a bug.\n"
                                + "Please contact the developer at https://github.com/ChristosMaragkos"
                        );
                }
            }
        }
    }

    private void WriteToRom(byte value, int offset = 0)
    {
        var realAddr = CurrentAddress + offset;
        if (realAddr >= Meta.Constants.MaxRomSize) // >= because MaxRomSize will throw as an index
            throw new SharpieRomSizeException(CurrentAddress);
        if (TouchedBytes[realAddr])
            throw new SharpieRomSizeException(
                $"Overlap error: Attempted to write to address 0x{realAddr:X2} ({realAddr}) twice. Check your .SPRITE / .ORG / .INCLUDE directives."
            );

        Rom[realAddr] = value;
        TouchedBytes[realAddr] = true;
    }

    private void WriteToRom(string? opcode) =>
        WriteToRom((byte)InstructionSet.GetOpcodeHex(opcode)!);

    private void WriteToRom(ushort value, int offset = 0)
    {
        var low = (byte)(value & 0x00FF);
        var high = (byte)((value & 0xFF00) >> 8); // low endian

        WriteToRom(low, offset);
        WriteToRom(high, offset + 1);
    }
}
