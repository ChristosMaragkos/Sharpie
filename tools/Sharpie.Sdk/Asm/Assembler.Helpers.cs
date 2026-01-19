using System.Text.RegularExpressions;

namespace Sharpie.Sdk.Asm;

public partial class Assembler
{
    private bool TryDefineLabel(string name, ushort address) =>
        CurrentScope.TryDefineLabel(name, address);

    private bool TryResolveLabel(string name, out ushort address)
    {
        foreach (var scope in _scopes)
            if (scope.LabelAddresses.TryGetValue(name, out address))
                return true;

        address = 0;
        return false;
    }

    private bool TryDefineConstant(string name, ushort value) =>
        CurrentScope.TryDefineConstant(name, value);

    private bool TryResolveConstant(string name, out ushort value)
    {
        foreach (var scope in _scopes)
            if (scope.Constants.TryGetValue(name, out value))
                return true;

        value = 0;
        return false;
    }

    private bool TryDefineEnum(string name) => CurrentScope.TryDefineEnum(name);

    private bool TryResolveEnum(string name)
    {
        foreach (var scope in _scopes)
            if (scope.Enums.TryGetValue(name, out _))
                return true;

        return false;
    }

    private bool TryDefineEnumMember(string enumName, string memberName, ushort value) =>
        CurrentScope.TryDefineEnumMember(enumName, memberName, value);

    private bool TryResolveEnumMember(string enumName, string memberName, out ushort value)
    {
        foreach (var scope in _scopes)
            if (
                scope.Enums.TryGetValue(enumName, out var members)
                && members.TryGetValue(memberName, out value)
            )
                return true;

        value = 0;
        return false;
    }

    private int? ParseNumberLiteral(
        string input,
        bool allowAddrPref,
        int lineNumber,
        int limit = ushort.MaxValue
    )
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        if (input.StartsWith('#'))
        {
            return ParseNote(input.Substring(1));
        }

        bool negative = input.StartsWith('-');
        if (negative)
            input = new string(input.Skip(1).ToArray());

        if (input.Contains("::"))
        {
            ushort value;
            string[] split;
            ResolveEnumValue(input, lineNumber, out split, out value);
            return !negative ? value : -value;
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

            return !negative ? result : -result;
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

        if (TryResolveConstant(arg, out var val))
            return (ushort)val;

        if (TryResolveLabel(arg, out var addr))
            return addr;

        var num = ParseNumberLiteral(arg, true, lineNum);
        if (num.HasValue && num.Value <= ushort.MaxValue)
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

        if (TryResolveConstant(arg, out var val))
            return (byte)val;

        var num = ParseNumberLiteral(arg, true, lineNumber: lineNum);
        if (num.HasValue && num.Value <= byte.MaxValue)
            return (byte)num;

        throw new AssemblySyntaxException(
            $"Invalid 8-bit value: '{arg}' (Note: the '$' prefix is only for addresses)",
            lineNum
        );
    }

    private byte ParseRegister(string arg, int lineNum)
    {
        var isEnum = arg.Contains("::");
        if (isEnum)
        {
            string[] split;
            ushort value;
            ResolveEnumValue(arg, lineNum, out split, out value);

            if (value > 0x0F)
                throw new AssemblySyntaxException(
                    $"Enum value {split[0]}::{split[1]} is not a valid register index - must be 0-15",
                    lineNum
                );
            return (byte)value;
        }

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

        if (TryResolveConstant(arg, out var constant))
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

    private void ResolveEnumValue(string arg, int lineNum, out string[] split, out ushort value)
    {
        split = arg.Split("::");
        if (split.Length != 2)
            throw new AssemblySyntaxException($"Unexpected token: {split.Last()}", lineNum);

        if (!TryResolveEnum(split[0]))
            throw new AssemblySyntaxException($"Unknown enum {split[0]}", lineNum);

        if (!TryResolveEnumMember(split[0], split[1], out value))
            throw new AssemblySyntaxException(
                $"Unknown value {split[1]} for enum {split[0]}",
                lineNum
            );
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
        var acdntl = match.Groups[2].Value.ToUpperInvariant();
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
