using System.Text.RegularExpressions;
using Sharpie.Sdk.Asm.Structuring;

namespace Sharpie.Sdk.Asm;

public partial class Assembler
{
    private static readonly Dictionary<string, ushort> BiosCallAddresses = new()
    {
        { "SYS_IDX_READ_VAL", 0xFA2A },
        { "SYS_STACKALLOC", 0xFA4E },
        { "SYS_FRAME_DELAY", 0xFA6F },
        { "SYS_IDX_WRITE_VAL", 0xFA7D },
        { "SYS_IDX_READ_REF", 0xFAA6 },
    };

    private ScopeLevel GetCurrentScope() =>
        CurrentRegion == null ? IRomBuffer.GlobalScope : CurrentRegion.CurrentScope;

    private bool TryDefineLabel(string name, ushort address, int lineNumber, bool global = false)
    {
        VerifyBiosPrefix(name, lineNumber);
        var currentScope =
            (CurrentRegion == null || global) ? IRomBuffer.GlobalScope : CurrentRegion.CurrentScope;
        int offset;
        switch (CurrentRegion)
        {
            case FixedRegionBuffer:
                offset = 0;
                break;
            case BankBuffer:
                var bnk = CurrentRegion as BankBuffer;
                offset = 18 * 1024;
                break;
            case SpriteAtlasBuffer:
                offset = (18 * 1024) + (32 * 1024);
                break;
            default:
                offset = 0;
                break;
        }
        return currentScope.TryDefineLabel(name, (ushort)(address + offset));
    }

    private bool TryResolveLabel(string name, out ushort address) =>
        GetCurrentScope().TryResolveLabel(name, out address);

    private bool TryDefineConstant(string name, ushort value, int lineNumber, bool global = false)
    {
        VerifyBiosPrefix(name, lineNumber);
        var currentScope =
            (CurrentRegion == null || global) ? IRomBuffer.GlobalScope : CurrentRegion.CurrentScope;
        return currentScope.TryDefineConstant(name, value);
    }

    private bool TryResolveConstant(string name, out ushort value) =>
        GetCurrentScope().TryResolveConstant(name, out value);

    private bool TryDefineEnum(string name, int lineNumber, bool global = false)
    {
        VerifyBiosPrefix(name, lineNumber);
        var currentScope =
            (CurrentRegion == null || global) ? IRomBuffer.GlobalScope : CurrentRegion.CurrentScope;
        return currentScope.TryDefineEnum(name);
    }

    private bool TryResolveEnum(string name) => GetCurrentScope().TryResolveEnum(name);

    private bool TryDefineEnumMember(
        string enumName,
        string memberName,
        ushort value,
        int lineNumber,
        bool global = false
    )
    {
        VerifyBiosPrefix(memberName, lineNumber);
        var currentScope =
            (CurrentRegion == null || global) ? IRomBuffer.GlobalScope : CurrentRegion.CurrentScope;
        return currentScope.TryDefineEnumMember(enumName, memberName, value);
    }

    private bool TryResolveEnumMember(string enumName, string memberName, out ushort value) =>
        GetCurrentScope().TryResolveEnumMember(enumName, memberName, out value);

    private static void VerifyBiosPrefix(string name, int lineNumber)
    {
        if (name.StartsWith("SYS_"))
            throw new AssemblySyntaxException(
                "The SYS_* prefix is reserved for BIOS calls - you cannot use it for constants, enums, enum members or labels.",
                lineNumber
            );
    }

    private void AddBiosLabels()
    {
        foreach (var kvp in BiosCallAddresses)
            IRomBuffer.GlobalScope.TryDefineLabel(kvp.Key, kvp.Value);
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
                throw new AssemblySyntaxException(
                    $"Numeric literal '{result}' is over the allowed limit of {limit}",
                    lineNumber
                );

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
