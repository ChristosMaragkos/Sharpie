using System.Text.RegularExpressions;

namespace Sharpie.Sdk.Asm;

public partial class Assembler
{
    private int? ParseNumberLiteral(string input, bool allowAddrPref, int limit = ushort.MaxValue)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (input.StartsWith('#'))
        {
            return ParseNote(input.Substring(1));
        }

        bool startsWithDollar = input.StartsWith('$'); // a dollar? oh, that's a big problem
        bool startsWith0x = input.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        bool startsWith0b = input.StartsWith("0b", StringComparison.OrdinalIgnoreCase);
        bool startsWithR = input.StartsWith("r", StringComparison.OrdinalIgnoreCase);

        string cleanArg = input;
        int style = 10;

        if (startsWith0x)
        {
            cleanArg = input.Substring(2);
            style = 16;
        }
        else if (startsWith0b)
        {
            cleanArg = input.Substring(2);
            style = 2;
        }
        else if (startsWithDollar)
        {
            cleanArg = input.Substring(1);
            style = 16;
        }
        else if (startsWithR)
        {
            cleanArg = input.Substring(1);
            limit = 15;
            style = 10;
        }
        // CAREFUL! just C4 is treated as a hex, but #C4 is treated as a note
        else if (Regex.IsMatch(input, "^[A-Fa-f]+$"))
        {
            style = 16;
        }

        try
        {
            int result = Convert.ToInt32(cleanArg, style);
            if (result > limit)
                return null;

            return result;
        }
        catch
        {
            return null;
        }
    }

    private ushort ParseWord(string arg, int lineNum)
    {
        var isChar = arg.StartsWith('\'') && arg.EndsWith('\'');
        if (isChar)
            return arg.Length == 3
                ? TextHelper.GetFontIndex(arg[1])
                : throw new AssemblySyntaxException($"Invalid character literal: {arg}", lineNum);

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
        var isChar = arg.StartsWith('\'') && arg.EndsWith('\'');
        if (isChar)
            return arg.Length == 3
                ? TextHelper.GetFontIndex(arg[1])
                : throw new AssemblySyntaxException($"Invalid character literal: {arg}", lineNum);

        if (Constants.TryGetValue(arg, out var val))
            return (byte)val;

        var num = ParseNumberLiteral(arg, true);
        if (num.HasValue && num.Value <= byte.MaxValue)
            return (byte)num;

        throw new AssemblySyntaxException(
            $"Invalid 8-bit value: '{arg}' (Note: the '$' prefix is only for addresses)",
            lineNum
        );
    }

    private byte ParseRegister(string arg, int lineNum)
    {
        var isChar = arg.StartsWith('\'') && arg.EndsWith('\'');
        if (isChar)
        {
            if (arg.Length != 3)
                throw new AssemblySyntaxException($"Invalid character literal: {arg}", lineNum);

            var charVal = TextHelper.GetFontIndex(arg[1]);
            if (charVal < 0 || charVal >= 16)
                throw new AssemblySyntaxException(
                    $"Register index {arg} ({charVal}) is not valid - must be 0-15",
                    lineNum
                );
        }

        var cleanArg = arg;
        if (arg.StartsWith('r') || arg.StartsWith('R'))
            cleanArg = arg.Substring(1);

        if (Constants.TryGetValue(arg, out var constant))
        {
            if (constant < 0 || constant >= 16)
                throw new AssemblySyntaxException(
                    $"Register index {constant} is not valid - must be 0-15",
                    lineNum
                );

            return (byte)constant;
        }

        if (!byte.TryParse(cleanArg, out byte parsed))
            throw new AssemblySyntaxException(
                $"Register index {arg} is not a valid number.",
                lineNum
            );

        if (parsed < 0 || parsed >= 16)
            throw new AssemblySyntaxException(
                $"Register index {parsed} is not valid - must be 0-15",
                lineNum
            );

        return parsed;
    }

    private int CalculateSpriteAddress(byte spriteIndex)
    {
        const int spriteSize = 32;
        const int romEnd = 0xE7FF;

        return (ushort)(romEnd - (spriteSize * (spriteIndex + 1)));
    }

    // fucking music theory, man.
    // the amount of googling this took was vomit inducing at best.
    private int? ParseNote(string input)
    {
        var match = Regex.Match(input.ToUpper(), @"^([A-G])(#|B)?(-?\d+)$"); // thank God for regex101

        if (!match.Success)
            return null;

        var noteChar = match.Groups[1].Value;
        var acdntl = match.Groups[2].Value;
        var octave = int.Parse(match.Groups[3].Value);

        var baseNote = noteChar switch
        {
            "C" => 0,
            "D" => 2,
            "E" => 4,
            "F" => 5,
            "G" => 7,
            "A" => 9,
            "B" => 11,
            _ => 0,
        };

        if (acdntl == "#")
            baseNote++;
        else if (acdntl == "B")
            baseNote--;

        var finalNote = (octave + 1) * 12 + baseNote;
        return (finalNote >= 0 && finalNote <= 127) ? finalNote : null;
    }
}
