namespace Sharpie.Sdk.Asm;

public partial class Assembler
{
    private const int MaxRomSize = 49152;
    private readonly byte[] Rom = new byte[MaxRomSize];

    public void Compile()
    {
        CurrentAddress = 0;
        int lineNum = 0;
        foreach (var token in Tokens)
        {
            lineNum++;
            if (!InstructionSet.IsOpcodeFamily(token.Opcode)!.Value)
            {
                WriteToRom(token.Opcode);
                CurrentAddress++;
            }
            for (int i = 0; i < token.Args!.Length; i++)
            {
                var arg = token.Args[i] ?? throw new NullReferenceException();
                if (arg.StartsWith('r')) // register index
                {
                    var index = int.Parse(arg.Substring(1));
                    if (index < 0 || index > 15)
                        throw new AssemblySyntaxException(
                            $"Invalid register index {index} (must be 0-15)",
                            lineNum
                        );
                    if (InstructionSet.IsOpcodeFamily(token.Opcode)!.Value)
                    {
                        WriteToRom(
                            (byte)(InstructionSet.GetOpcodeHex(token.Opcode)!.Value | index)
                        );
                        CurrentAddress++;
                    }
                    else
                    {
                        WriteToRom((byte)index);
                        CurrentAddress++;
                    }
                }
                else if (arg.StartsWith('\'') && arg.EndsWith('\''))
                {
                    char c = arg[1];
                    byte fontIdx = TextHelper.GetFontIndex(c);
                    WriteToRom(fontIdx);
                    CurrentAddress++;
                }
                else if (IsValidInteger(arg, out var numArg, out var isAddr)) // number
                {
                    if (numArg > ushort.MaxValue)
                        throw new AssemblySyntaxException(
                            $"Number {numArg} cannot be beyond the 16-bit integer limit ({ushort.MaxValue})",
                            lineNum
                        );
                    if (numArg < 0)
                        throw new AssemblySyntaxException(
                            $"Number {numArg} cannot be negative",
                            lineNum
                        );

                    if (isAddr || numArg > byte.MaxValue) // memory address
                    {
                        WriteToRom((ushort)numArg!);
                        CurrentAddress += 2;
                    }
                    else
                    {
                        WriteToRom((byte)numArg!);
                        CurrentAddress++;
                    }
                }
                else if (LabelToMemAddr.TryGetValue(arg, out var addr)) // label
                {
                    WriteToRom(addr);
                    CurrentAddress += 2;
                }
                else if (Constants.TryGetValue(arg, out var constant)) // constant reference (.DEF)
                {
                    if (constant > byte.MaxValue)
                    {
                        WriteToRom((byte)constant);
                        CurrentAddress++;
                    }
                    else
                    {
                        WriteToRom(constant);
                        CurrentAddress += 2;
                    }
                }
                else
                {
                    throw new AssemblySyntaxException(
                        $"Unexpected token: {token.ToString()}",
                        lineNum
                    );
                }
            }
        }
    }

    private void WriteToRom(byte value, int offset = 0)
    {
        var realAddr = CurrentAddress + offset;
        if (realAddr >= MaxRomSize) // >= because MaxRomSize will throw as an index
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

    private bool IsValidInteger(string numString, out int? number, out bool isAddr)
    {
        isAddr = numString.StartsWith('$');
        var cleanString = isAddr ? numString.Substring(1) : numString;
        number = null;
        if (isAddr)
        {
            try
            {
                number = Convert.ToInt32(cleanString, 16);
                return true;
            }
            catch
            {
                return false;
            }
        }
        if (cleanString.StartsWith("0x") || cleanString.StartsWith("0X")) // hex
        {
            try
            {
                number = Convert.ToInt32(cleanString, 16);
                return true;
            }
            catch
            {
                return false;
            }
        }
        else
        {
            if (!int.TryParse(cleanString, out int parsed))
                return false;

            number = parsed;
            return true;
        }
    }
}
