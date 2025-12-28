using System.Text.RegularExpressions;

namespace Sharpie.Sdk.Asm;

public partial class Assembler
{
    public readonly byte[] Rom = new byte[Meta.Constants.MaxRomSize];

    public void Compile()
    {
        Console.WriteLine("Assembler: Compiling file...");
        CurrentAddress = 0;
        int lineNum = 0;
        foreach (var token in Tokens)
        {
            lineNum = token.SourceLine!.Value;

            if (token.Args == null)
                throw new AssemblySyntaxException(
                    $"Expected arguments for opcode {token.Opcode}",
                    lineNum
                );

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

                        if (target < CurrentAddress)
                            throw new SharpieRomSizeException(
                                $"Sprite #{spriteIndex} overlaps existing code"
                            );

                        CurrentAddress = target;
                        break;

                    case ".DB":
                    case ".BYTES":
                    case ".DATA":
                        foreach (var arg in token.Args)
                        {
                            WriteToRom(ParseByte(arg, lineNum));
                            CurrentAddress++;
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

            if (isFam)
            {
                var arg = token.Args[argIndex];
                argIndex++;
                int regIdx = ParseRegister(arg, lineNum);
                WriteToRom((byte)(opHex | regIdx));
            }
            else
            {
                WriteToRom((byte)opHex);
            }
            CurrentAddress++;

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
                            WriteToRom((byte)(rA << 4 | rB));
                            p++;
                        }
                        else
                        {
                            WriteToRom(ParseRegister(token.Args[argIndex++], lineNum));
                        }
                        CurrentAddress++;
                        break;

                    case 'W':
                        WriteToRom(ParseWord(token.Args[argIndex++], lineNum));
                        CurrentAddress += 2;
                        break;
                    case 'B':
                        WriteToRom(ParseByte(token.Args[argIndex++], lineNum));
                        CurrentAddress++;
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

    private ushort ParseWord(string arg, int lineNum)
    {
        if (Constants.TryGetValue(arg, out var val))
            return (ushort)val;

        if (LabelToMemAddr.TryGetValue(arg, out var addr))
            return addr;

        var num = ParseNumberLiteral(arg, true);
        if (num.HasValue && num.Value >= 0 && num.Value <= ushort.MaxValue)
            return (ushort)num;

        throw new AssemblySyntaxException(
            $"Invalid unsigned 16-bit value or unresolved symbol: '{arg}'",
            lineNum
        );
    }

    private byte ParseByte(string arg, int lineNum)
    {
        if (Constants.TryGetValue(arg, out var val))
            return (byte)val;

        var num = ParseNumberLiteral(arg, false);
        if (num.HasValue && num.Value >= 0 && num.Value <= byte.MaxValue)
            return (byte)num;

        throw new AssemblySyntaxException(
            $"Invalid unsigned 8-bit value: '{arg}' (Note: the '$' prefix is only for addresses)",
            lineNum
        );
    }

    private byte ParseRegister(string arg, int lineNumber)
    {
        var cleanArg = arg;
        if (arg.StartsWith('r') || arg.StartsWith('R'))
            cleanArg = arg.Substring(1);

        if (Constants.TryGetValue(arg, out var constant))
        {
            if (constant < 0 || constant >= 16)
                throw new AssemblySyntaxException(
                    $"Register index {constant} is not valid - must be 0-15",
                    lineNumber
                );

            return (byte)constant;
        }

        if (!byte.TryParse(cleanArg, out byte parsed))
            throw new AssemblySyntaxException(
                $"Register index {arg} is not a valid number.",
                lineNumber
            );

        if (parsed < 0 || parsed >= 16)
            throw new AssemblySyntaxException(
                $"Register index {parsed} is not valid - must be 0-15",
                lineNumber
            );

        return parsed;
    }

    private void WriteToRom(byte value, int offset = 0)
    {
        var realAddr = CurrentAddress + offset;
        if (realAddr >= Meta.Constants.MaxRomSize) // >= because MaxRomSize will throw as an index
            throw new SharpieRomSizeException(CurrentAddress);

        Rom[realAddr] = value;
    }

    private void WriteToRom(string? opcode) =>
        WriteToRom((byte)InstructionSet.GetOpcodeHex(opcode)!);

    private void WriteToRom(ushort value)
    {
        var low = (byte)(value & 0x00FF);
        var high = (byte)((value & 0xFF00) >> 8); // low endian

        WriteToRom(low);
        WriteToRom(high, 1);
    }

    private int? ParseNumberLiteral(string input, bool allowAddrPref)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var isAddr = input.StartsWith('$');
        if (isAddr && !allowAddrPref)
            return null;

        var cleanArg = isAddr ? input.Substring(1) : input;
        var style = 10;

        if (cleanArg.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            cleanArg = cleanArg.Substring(2);
            style = 16;
        }
        else if (isAddr || Regex.Match(cleanArg, "[A-F]|[a-f]").Success)
        {
            style = 16;
        }

        try
        {
            return Convert.ToInt32(cleanArg, style);
        }
        catch
        {
            return null;
        }
    }

    private int CalculateSpriteAddress(byte spriteIndex)
    {
        const int spriteSize = 32;
        const int romEnd = 0xE000;

        return (ushort)(romEnd - (spriteSize * (spriteIndex + 1)));
    }
}
