using Sharpie.Sdk.Asm.Structuring;

namespace Sharpie.Sdk.Asm;

public partial class Assembler
{
    private readonly bool _firmwareMode = false;

    public Assembler(bool firmwareMode)
    {
        _firmwareMode = firmwareMode;
    }

    public byte[] Compile()
    {
        Console.WriteLine("Assembler: Compiling file...");
        int lineNum = 0;

        var rom = new List<byte>();

        if (!_firmwareMode)
        {
            if (!AllRegions.TryGetValue("FIXED", out var fxd))
                throw new AssemblySyntaxException("No definition for fixed region found.");

            CurrentRegion = fxd;
            rom.AddRange(ProcessTokens(ref lineNum, fxd));

            var banks = AllRegions.Values.OfType<BankBuffer>().OrderBy(b => b.BankId).ToArray();
            if (banks.Length != 0)
            {
                for (int i = 0; i < banks.Length; i++)
                {
                    CurrentRegion = banks[i];
                    rom.AddRange(ProcessTokens(ref lineNum, banks[i]));
                }
            }
            else
            {
                var bank = AllRegions["BANK_0"] = new BankBuffer();
                rom.AddRange(ProcessTokens(ref lineNum, bank));
            }

            if (!AllRegions.TryGetValue("SPRITE_ATLAS", out var sprt))
                sprt = AllRegions["SPRITE_ATLAS"] = new SpriteAtlasBuffer(); // just add an empty one

            CurrentRegion = sprt;
            rom.AddRange(ProcessTokens(ref lineNum, sprt));
        }
        else
        {
            if (!AllRegions.TryGetValue("FIRMWARE", out var frmwr))
                throw new AssemblySyntaxException(
                    "How did you manage to exit the firmware region bank?"
                );

            rom.AddRange(ProcessTokens(ref lineNum, frmwr));
        }

        return rom.ToArray();
    }

    private byte[] ProcessTokens(ref int lineNum, IRomBuffer buffer)
    {
        var scopeOpens = 1;
        var scopeCloses = 1;
        foreach (var token in buffer.Tokens)
        {
            if (token.Opcode == ".SPRITE" || token.Opcode == ".DEF" || token.Opcode == ".ORG")
                continue;
            lineNum = token.SourceLine!.Value;
            buffer.SetCursor(token.Address!.Value);

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
                    case ".SCOPE":
                        scopeOpens++;
                        buffer.Scopes.Push(buffer.AllScopes[scopeOpens]);
                        break;

                    case ".ENDSCOPE":
                        scopeCloses++;
                        ExitScope();
                        break;

                    case ".DB":
                    case ".BYTES":
                    case ".DATA":
                        for (int i = 0; i < token.Args.Length; i++)
                            WriteToRom(ParseByte(token.Args[i], lineNum), buffer, i);
                        break;

                    case ".DW":
                    case ".WORDS":
                        for (int i = 0; i < token.Args.Length; i++)
                        {
                            WriteToRom(ParseWord(token.Args[i], lineNum), buffer, i * 2);
                        }
                        break;
                }
                continue;
            }

            var pattern = InstructionSet.GetOpcodePattern(token.Opcode);
            var opHex = InstructionSet.GetOpcodeHex(token.Opcode);
            var isFam = InstructionSet.IsOpcodeFamily(token.Opcode);
            var argIndex = 0;

            var localOffset = 0;

            if (isFam)
            {
                var arg = token.Args[argIndex];
                argIndex++;
                int regIdx = ParseRegister(arg, lineNum);
                WriteToRom((byte)(opHex | regIdx), buffer, localOffset);
                localOffset++;
            }
            else
            {
                WriteToRom((byte)opHex, buffer, localOffset++);
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
                            WriteToRom((byte)(rA << 4 | rB), buffer, localOffset++);
                            p++;
                        }
                        else
                        {
                            WriteToRom(
                                ParseRegister(token.Args[argIndex++], lineNum),
                                buffer,
                                localOffset++
                            );
                        }
                        break;

                    case 'W':
                        WriteToRom(ParseWord(token.Args[argIndex++], lineNum), buffer, localOffset);
                        localOffset += 2;
                        break;
                    case 'B':
                        WriteToRom(
                            ParseByte(token.Args[argIndex++], lineNum),
                            buffer,
                            localOffset++
                        );
                        break;
                    default:
                        throw new AssemblySyntaxException(
                            $"The SDK definition of the {token.Opcode} instruction contains a bug.\n"
                                + "Please contact the developer at https://github.com/ChristosMaragkos"
                        );
                }
            }
        }
        if (scopeOpens != scopeCloses)
            throw new AssemblySyntaxException(
                $"A .SCOPE directive was left without a matching .ENSCOPE in bank {buffer.Name}"
            );

        return buffer.ByteBuffer;
    }

    private void WriteToRom(byte value, IRomBuffer buffer, int offset = 0)
    {
        var realAddr = buffer.Cursor + offset;

        buffer.SetCursor(realAddr);
        buffer.WriteByte(value);
        buffer.SetCursor(realAddr - offset);
    }

    private void WriteToRom(ushort value, IRomBuffer buffer, int offset = 0)
    {
        var low = (byte)value;
        var high = (byte)(value >> 8);

        WriteToRom(low, buffer, offset);
        WriteToRom(high, buffer, offset + 1);
    }
}
