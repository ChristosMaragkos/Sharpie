using System.Text.RegularExpressions;
using Sharpie.Sdk.Asm.Structuring;

namespace Sharpie.Sdk.Asm;

public partial class Assembler
{
    private static readonly char[] CommonDelimiters = [',', ' '];

    private IEnumerable<string>? FileContents { get; set; }

    private static readonly char[] DisallowedEnumChars = [':', ',', '#', '=', ' ', '\'', '"'];
    private static readonly List<TokenLine> FirmwareModeTokens = new();

    private void AddToken(TokenLine token)
    {
        if (CurrentRegion == null)
            throw new AssemblySyntaxException(
                $"Only enum, label and constant definitions are allowed outside of regions.",
                token.SourceLine!.Value
            );

        CurrentRegion.Tokens.Add(token);
    }

    private string? _currentEnum = null;
    private ushort _currentEnumVal;
    private bool _globalMode;

    private IRomBuffer? CurrentRegion = null;
    private readonly Dictionary<string, IRomBuffer> AllRegions = new();

    private void NewScope()
    {
        if (CurrentRegion == null && !_firmwareMode)
            throw new AssemblySyntaxException("Cannot enter local scope outside of a region.");
        CurrentRegion!.NewScope();
    }

    private void ExitScope()
    {
        if (CurrentRegion == null)
            throw new AssemblySyntaxException("Cannot exit local scope outside of a region.");
        CurrentRegion!.ExitScope();
    }

    private ScopeLevel? CurrentScope =>
        CurrentRegion == null ? IRomBuffer.GlobalScope : CurrentRegion.CurrentScope;
    private bool IsInLocalScope => CurrentRegion == null ? false : CurrentRegion.Scopes.Count > 2;

    private byte[] ReadFile()
    {
        if (FileContents == null)
            throw new NullReferenceException("File contents are null. Check your file path.");

        Console.WriteLine("Assembler: Reading file...");

        var lineNum = 0;
        string cleanLine;
        AddBiosLabels();
        if (_firmwareMode)
        {
            CurrentRegion = new FirmwareBuffer();
            AllRegions["FIRMWARE"] = CurrentRegion;
        }

        foreach (var line in FileContents!)
        {
            var tokenLine = new TokenLine();
            lineNum++;
            tokenLine.SourceLine = lineNum;
            // Keep the original (mixed-case) trimmed line so string literals
            // inside .STR / .DB directives preserve their casing before we
            // upper-case everything else for opcode/keyword matching.
            var originalLine = line.Trim();
            cleanLine = originalLine.ToUpper();
            RemoveComment(ref cleanLine);
            // Mirror the comment removal on the original line so that ParseDbArgs /
            // CountDbBytes never see text that follows a ';' comment marker.
            RemoveComment(ref originalLine);
            if (IsLineEmpty(cleanLine))
                continue;

            if (line == "END-INCLUDE-ABCDEFGHIJKLMNOPQRSTUVWXYZ-BANANA")
            {
                lineNum = 0;
                continue;
            }

            ParseGlobal(ref cleanLine, lineNum);
            if (IsLineEmpty(cleanLine))
                continue;

            ParseRegion(ref cleanLine, lineNum);
            if (IsLineEmpty(cleanLine))
                continue;

            ParseScope(ref cleanLine, lineNum);
            if (IsLineEmpty(cleanLine))
                continue;

            ParseEnumDefinition(ref cleanLine, lineNum);
            if (IsLineEmpty(cleanLine))
                continue;

            ParseConstantDefinition(ref cleanLine, lineNum);
            if (IsLineEmpty(cleanLine)) // should be empty but oh well
                continue;

            // Pass originalLine so quoted string content is not upper-cased.
            ParseStringDirective(ref cleanLine, originalLine, lineNum);
            if (IsLineEmpty(cleanLine))
                continue;

            RemoveLabel(ref cleanLine, lineNum);
            if (IsLineEmpty(cleanLine))
                continue;

            Tokenize(cleanLine, originalLine, ref tokenLine, lineNum);

            if (tokenLine.ArePropertiesNull())
                continue;
            if (tokenLine.Opcode != "ALT")
                AddToken(tokenLine); // ALT is added right when tokenizing
        }

        return Compile();

        bool IsLineEmpty(string line) => string.IsNullOrWhiteSpace(line);
    }

    private void ParseGlobal(ref string cleanLine, int lineNum)
    {
        if (cleanLine.StartsWith(".GLOBAL"))
        {
            _globalMode = true;
            cleanLine = cleanLine.Substring(".GLOBAL".Length).Trim();
        }
        else if (cleanLine.StartsWith(".ENDGLOBAL"))
        {
            _globalMode = false;
            cleanLine = cleanLine.Substring(".ENDGLOBAL".Length).Trim();
        }
    }

    private void ParseRegion(ref string cleanLine, int lineNum)
    {
        if (cleanLine.StartsWith(".REGION"))
        {
            AssertNoFirmware();
            if (_firmwareMode)
                throw new AssemblySyntaxException(
                    "Regions are implicit in firmware mode.",
                    lineNum
                );
            if (CurrentRegion != null)
                throw new AssemblySyntaxException($"Cannot create nested regions.", lineNum);

            var parts = cleanLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new AssemblySyntaxException(
                    "Directive .REGION expected a region name. Try: 'FIXED', 'BANK_0', 'SPRITE_ATLAS'",
                    lineNum
                );

            if (parts.Length > 2)
                throw new AssemblySyntaxException($"Unexpected token: {parts.Last()}", lineNum);

            var regionName = parts[1];
            SwitchCurrentRegion(regionName, lineNum);
        }
        else if (cleanLine.StartsWith(".ENDREGION"))
        {
            AssertNoFirmware();
            if (CurrentRegion == null)
                throw new AssemblySyntaxException(
                    "Directive .ENDREGION could not find an opening .REGION",
                    lineNum
                );
            CurrentRegion = null;
        }

        void AssertNoFirmware()
        {
            if (_firmwareMode)
                throw new AssemblySyntaxException(
                    "Regions are implicit in firmware mode.",
                    lineNum
                );
        }
    }

    private void SwitchCurrentRegion(string regionName, int lineNum)
    {
        var bankId = 0;
        if (AllRegions.TryGetValue(regionName, out var existing))
        {
            CurrentRegion = existing;
            return;
        }
        if (regionName.StartsWith("BANK"))
        {
            var parts = regionName.Split("_");
            if (parts.Length != 2)
                throw new AssemblySyntaxException($"Unexpected token: {parts.Last()}", lineNum);

            regionName = parts[0]; // "BANK"
            bankId = ParseByte(parts[1], lineNum);
        }
        IRomBuffer next;
        switch (regionName)
        {
            case "FIXED":
                next = new FixedRegionBuffer();
                break;
            case "BANK":
                regionName += $"_{bankId}";
                next = new BankBuffer();
                break;
            case "SPRITE_ATLAS":
                next = new SpriteAtlasBuffer();
                break;
            default:
                throw new AssemblySyntaxException(
                    $"Invalid region name {regionName} - Valid region names are: FIXED, BANK_<bank-number>, SPRITE_ATLAS",
                    lineNum
                );
        }
        AllRegions[regionName] = next;
        CurrentRegion = next;
    }

    private void ParseScope(ref string cleanLine, int lineNum)
    {
        if (cleanLine.StartsWith(".SCOPE"))
        {
            NewScope();
            cleanLine = cleanLine.Substring(".SCOPE".Length).Trim(); // allow labels and such after scope start
            AddToken(
                new TokenLine
                {
                    Opcode = ".SCOPE",
                    SourceLine = lineNum,
                    Address = CurrentRegion!.Cursor,
                    Args = [],
                }
            );
        }
        else if (cleanLine.StartsWith(".ENDSCOPE"))
        {
            if (!IsInLocalScope)
                throw new AssemblySyntaxException(
                    "No matching .SCOPE found for .ENDSCOPE",
                    lineNum
                );

            ExitScope();
            cleanLine = cleanLine.Substring(".ENDSCOPE".Length).Trim();
            AddToken(
                new TokenLine
                {
                    Opcode = ".ENDSCOPE",
                    SourceLine = lineNum,
                    Address = CurrentRegion!.Cursor,
                    Args = [],
                }
            );
        }
    }

    private void ParseEnumDefinition(ref string cleanLine, int lineNum)
    {
        if (cleanLine.StartsWith(".ENUM"))
        {
            var parts = cleanLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (_currentEnum != null)
                throw new AssemblySyntaxException(
                    $"Cannot declare enum {parts[1]} within another enum",
                    lineNum
                );

            if (parts.Length != 2)
                throw new AssemblySyntaxException(
                    $"Unexpected second argument to .ENUM directive: {parts.Last()}",
                    lineNum
                );

            var name = parts[1];

            if (
                TryResolveLabel(name, out _)
                || TryResolveConstant(name, out _)
                || !TryDefineEnum(name, lineNum, _globalMode)
            )
                throw new AssemblySyntaxException(
                    $"Enum named {parts[1]} is already declared.",
                    lineNum
                );

            _currentEnum = parts[1];
            _currentEnumVal = 0;
            cleanLine = string.Empty; // we already know we have the correct amount of args if we haven't thrown
        }
        else if (cleanLine.StartsWith(".ENDENUM"))
        {
            if (_currentEnum == null)
                throw new AssemblySyntaxException("No matching .ENUM found for .ENDENUM", lineNum);
            _currentEnum = null;
            cleanLine = cleanLine.Substring(".ENDENUM".Length).Trim();
        }
        else if (_currentEnum != null)
        {
            var parts = cleanLine
                .Split('=', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();

            if (parts.Length > 2)
                throw new AssemblySyntaxException($"Unexpected token: {parts.Last()}", lineNum);

            var enumMember = parts[0];

            if (parts.Length == 1)
            {
                var whitespaceSplit = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (whitespaceSplit.Length != 1)
                    throw new AssemblySyntaxException(
                        $"Unexpected token: {whitespaceSplit.Last()}",
                        lineNum
                    );

                if (enumMember.ContainsAny(DisallowedEnumChars))
                {
                    var invalidChar = enumMember.First(c => DisallowedEnumChars.Contains(c));
                    throw new AssemblySyntaxException(
                        $"Unexpected character in enum value {enumMember} : {invalidChar}",
                        lineNum
                    );
                }

                if (
                    !TryDefineEnumMember(
                        _currentEnum,
                        enumMember,
                        _currentEnumVal++,
                        lineNum,
                        _globalMode
                    )
                )
                    throw new AssemblySyntaxException(
                        $"Member {enumMember} already defined for enum {_currentEnum}",
                        lineNum
                    );
            }
            else // always two since we throw otherwise
            {
                var whitespaceSplit = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (whitespaceSplit.Length != 1)
                    throw new AssemblySyntaxException(
                        $"Unexpected token: {whitespaceSplit.Last()}",
                        lineNum
                    );

                var value = ParseWord(parts[1], lineNum);

                if (!TryDefineEnumMember(_currentEnum, enumMember, value, lineNum, _globalMode))
                    throw new AssemblySyntaxException(
                        $"Member {enumMember} already defined for enum {_currentEnum}",
                        lineNum
                    );

                _currentEnumVal = (ushort)(value + 1);
            }
            cleanLine = string.Empty;
        }
    }

    private void ParseConstantDefinition(ref string line, int lineNumber)
    {
        if (!line.StartsWith(".DEF"))
            return;

        var args = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length > 3)
            throw new AssemblySyntaxException($"Unexpected token: {args.Last()}", lineNumber);
        if (args.Length < 3)
            throw new AssemblySyntaxException(
                "Expected constant definition for directive .DEF",
                lineNumber
            );

        for (var i = 0; i < args.Length; i++)
            args[i] = args[i].Trim(CommonDelimiters).Trim();

        var (name, valueStr) = (args[1], args[2]);

        var value = ParseNumberLiteral(valueStr, true, lineNumber);
        if (value == null)
            throw new AssemblySyntaxException(
                $"Unexpected token: {valueStr} - expected a number",
                lineNumber
            );

        if (
            TryResolveLabel(name, out _)
            || !TryDefineConstant(name, (ushort)value, lineNumber, _globalMode)
            || TryResolveEnum(name)
        )
            throw new AssemblySyntaxException($"Constant {name} is already declared", lineNumber);

        if (value > ushort.MaxValue)
            throw new AssemblySyntaxException(
                $"Number {value} cannot be larger than {ushort.MaxValue}.",
                lineNumber
            );

        line = string.Empty;
    }

    private void ParseStringDirective(ref string line, string originalLine, int lineNumber)
    {
        if (!line.StartsWith(".STR"))
            return;

        // Locate quotes in the ORIGINAL (mixed-case) line so string content is preserved.
        var firstQuote = originalLine.IndexOf('"');
        if (firstQuote == -1)
            throw new AssemblySyntaxException(
                "String literal must be wrapped in double quotes",
                lineNumber
            );

        var sb = new System.Text.StringBuilder();
        var j = firstQuote + 1;
        while (j < originalLine.Length && originalLine[j] != '"')
        {
            if (
                originalLine[j] == '\\'
                && j + 1 < originalLine.Length
                && originalLine[j + 1] == '"'
            )
            {
                sb.Append('"');
                j += 2;
            }
            else
            {
                sb.Append(originalLine[j]);
                j++;
            }
        }
        if (j >= originalLine.Length)
            throw new AssemblySyntaxException(
                "String literal must be wrapped in double quotes",
                lineNumber
            );
        var lastQuote = j;

        var message = sb.ToString();

        var coordPart = line.Substring(4, firstQuote - 4).Trim();
        var coordArgs = coordPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (coordArgs.Length != 2)
            throw new AssemblySyntaxException(
                "Expected X and Y coordinates before the string",
                lineNumber
            );
        for (var i = 0; i < coordArgs.Length; i++)
            coordArgs[i] = coordArgs[i].Trim(CommonDelimiters);

        AddToken(
            new TokenLine
            {
                Opcode = "SETCRS",
                Args = new[] { coordArgs[0], coordArgs[1] },
                SourceLine = lineNumber,
                Address = CurrentRegion!.Cursor,
            }
        );
        CurrentRegion!.AdvanceCursor(InstructionSet.GetOpcodeLength("SETCRS"));

        var delta = InstructionSet.GetOpcodeLength("TEXT");
        foreach (var c in message)
        {
            TokenLine tl = new()
            {
                Opcode = "TEXT",
                Args = new[] { TextHelper.AsciiToByte(c).ToString() },
                SourceLine = lineNumber,
                Address = CurrentRegion!.Cursor,
            };
            AddToken(tl);
            CurrentRegion!.AdvanceCursor(delta);
        }

        var remainder = line.Substring(lastQuote + 1).TrimStart(CommonDelimiters).Trim();
        var extraArgs = remainder.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var arg in extraArgs)
        {
            var cleanArg = arg.Trim(CommonDelimiters).Trim();
            var value = ParseRegister(cleanArg, lineNumber);
            AddToken(
                new()
                {
                    Opcode = "ALT",
                    Args = Array.Empty<string>(),
                    SourceLine = lineNumber,
                    Address = CurrentRegion?.Cursor,
                }
            );
            CurrentRegion!.AdvanceCursor(InstructionSet.GetOpcodeLength("ALT"));

            AddToken(
                new()
                {
                    Opcode = "TEXT",
                    Args = new[] { cleanArg },
                    SourceLine = lineNumber,
                    Address = CurrentRegion?.Cursor,
                }
            );
            CurrentRegion!.AdvanceCursor(InstructionSet.GetOpcodeLength("TEXT"));
        }

        line = string.Empty;
    }

    public byte[] LoadFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"No file by name \"{filePath}\" was found.");
        if (!filePath.EndsWith(".asm"))
            throw new Exception($"File \"{filePath}\" is not in the \".asm\" format.");
        var initialFile = File.ReadAllLines(filePath);
        return Assemble(PreProcess(initialFile, Path.GetDirectoryName(filePath)!));
    }

    public byte[] LoadRawAsm(string asm)
    {
        string[] lines = asm.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        return Assemble(lines);
    }

    private byte[] Assemble(IEnumerable<string> asmCode)
    {
        FileContents = asmCode;
        Console.WriteLine("Assembler: Loading file...");
        return ReadFile();
    }

    private static void RemoveComment(ref string line)
    {
        if (!line.Contains(';'))
            return;
        var comment = Regex.Match(line, ";");
        if (comment.Success)
        {
            line = line.Remove(comment.Index).Trim();
        }
    }

    private void RemoveLabel(ref string line, int lineNumber)
    {
        var labelRegex = Regex.Match(line, @"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*:(?!:)");
        if (!labelRegex.Success)
            return;

        var label = labelRegex.Groups[1].Value;

        if (
            !TryDefineLabel(label, (ushort)CurrentRegion!.Cursor, lineNumber, _globalMode)
            || TryResolveConstant(label, out _)
            || TryResolveEnum(label)
        )
            throw new AssemblySyntaxException($"Label {label} is already declared", lineNumber);

        line = line.Substring(labelRegex.Index + labelRegex.Length).Trim();
    }

    private static string[] SplitArgsPreservingQuotedLiterals(string line, int lineNumber)
    {
        var tokens = new List<string>();
        var tokenStart = -1;
        char activeQuote = '\0';

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (activeQuote != '\0')
            {
                if (ch == activeQuote)
                    activeQuote = '\0';
                continue;
            }

            if (ch == '\'' || ch == '"')
            {
                if (tokenStart < 0)
                    tokenStart = i;
                activeQuote = ch;
                continue;
            }

            if (ch == ',' || char.IsWhiteSpace(ch))
            {
                if (tokenStart >= 0)
                {
                    tokens.Add(line.Substring(tokenStart, i - tokenStart));
                    tokenStart = -1;
                }
                continue;
            }

            if (tokenStart < 0)
                tokenStart = i;
        }

        if (activeQuote != '\0')
            throw new AssemblySyntaxException("Unterminated quoted literal", lineNumber);

        if (tokenStart >= 0)
            tokens.Add(line.Substring(tokenStart));

        return tokens
            .Select(str => str.Trim(CommonDelimiters))
            .Where(str => !string.IsNullOrWhiteSpace(str))
            .ToArray();
    }

    private void Tokenize(string line, string originalLine, ref TokenLine tokenLine, int lineNumber)
    {
        var args = SplitArgsPreservingQuotedLiterals(line, lineNumber);

        tokenLine.Opcode = args[0];
        tokenLine.Args = args.Skip(1).ToArray();

        if (tokenLine.Opcode.StartsWith('.'))
        {
            switch (tokenLine.Opcode.ToUpper())
            {
                case ".ORG":
                    if (args.Length > 2)
                        throw new AssemblySyntaxException(
                            $"Unexpected token: {args.Last()}",
                            lineNumber
                        );
                    else if (args.Length < 2)
                        throw new AssemblySyntaxException(
                            "Directive .ORG expected a valid memory address",
                            lineNumber
                        );
                    else
                    {
                        if (CurrentRegion == null)
                            throw new AssemblySyntaxException(
                                ".ORG is disabled outside regions as no code is allowed.",
                                lineNumber
                            );
                        var addr = ParseWord(args[1], lineNumber);
                        if (addr >= CurrentRegion!.Size)
                            throw new AssemblySyntaxException(
                                $"Directive .ORG cannot jump to address {addr} as buffer {CurrentRegion!.Name} has a maximum size of {CurrentRegion!.Size}",
                                lineNumber
                            );

                        CurrentRegion.SetCursor(addr);
                    }

                    break;

                case ".SPRITE":
                    if (args.Length > 2)
                        throw new AssemblySyntaxException(
                            $"Unexpected token: {args.Last()}",
                            lineNumber
                        );
                    else if (args.Length < 2)
                        throw new AssemblySyntaxException(
                            "Directive .SPRITE expected a valid sprite index 0-255",
                            lineNumber
                        );
                    else
                    {
                        if (CurrentRegion is not SpriteCapableBuffer spriteBuf)
                            throw new AssemblySyntaxException(
                                ".SPRITE is only enabled within the sprite atlas region.",
                                lineNumber
                            );

                        var spriteIndex = ParseByte(args[1], lineNumber);
                        spriteBuf.PositionCursor(spriteIndex);
                        tokenLine.Address = spriteBuf.Cursor;
                    }

                    break;

                case ".DB":
                case ".BYTES":
                case ".DATA":
                {
                    tokenLine.Address = CurrentRegion?.Cursor;
                    // Reparse args from the original (mixed-case) line so that:
                    //   1. Quoted string literals are kept as a single token.
                    //   2. Case inside strings is preserved for raw ASCII emission.
                    tokenLine.Args = ParseDbArgs(originalLine, lineNumber);
                    var byteCount = CountDbBytes(originalLine, lineNumber);
                    CurrentRegion!.AdvanceCursor(byteCount);
                    break;
                }

                case ".DW":
                    tokenLine.Address = CurrentRegion?.Cursor;
                    CurrentRegion!.AdvanceCursor(2 * (args.Length - 1));
                    break;

                case ".REGION":
                case ".ENDREGION":
                case ".GLOBAL":
                case ".ENDGLOBAL":
                    tokenLine.Opcode = null;
                    tokenLine.Args = null;
                    break;
                case ".STR":
                case ".DEF":
                    break;

                default:
                    throw new AssemblySyntaxException(
                        $"Unknown directive: {tokenLine.Opcode}",
                        lineNumber
                    );
            }
            return; // no need to check for an opcode
        }

        if (args[0] == "ALT" && args.Length > 1)
        {
            AddToken(
                new TokenLine
                {
                    Opcode = "ALT",
                    Args = Array.Empty<string>(),
                    SourceLine = lineNumber,
                    Address = CurrentRegion!.Cursor,
                }
            );
            CurrentRegion!.AdvanceCursor(InstructionSet.GetOpcodeLength("ALT"));

            var remainingLine = string.Join(' ', args.Skip(1));
            var nextToken = new TokenLine
            {
                SourceLine = lineNumber,
                Address = CurrentRegion?.Cursor,
            };
            Tokenize(remainingLine, remainingLine, ref nextToken, lineNumber);
            AddToken(nextToken);
            return;
        }

        if (!InstructionSet.IsValidOpcode(tokenLine.Opcode))
            throw new AssemblySyntaxException($"Invalid Opcode: {tokenLine.Opcode}", lineNumber);

        if (args.Length - 1 != InstructionSet.GetOpcodePattern(tokenLine.Opcode).Length)
            throw new AssemblySyntaxException(
                $"Invalid argument count for opcode {tokenLine.Opcode}: expected {InstructionSet.GetOpcodeWords(tokenLine.Opcode)} but found {args.Length - 1}",
                lineNumber
            );

        if (!tokenLine.Address.HasValue)
            tokenLine.Address = CurrentRegion?.Cursor;

        CurrentRegion!.AdvanceCursor(InstructionSet.GetOpcodeLength(tokenLine.Opcode));
    }

    /// <summary>
    /// Parses the argument list of a .DB / .BYTES / .DATA directive from the original
    /// (mixed-case, pre-ToUpper) line.  Quoted string literals are returned as single
    /// tokens (including their surrounding quotes) so the compiler can handle them as
    /// strings.  All other tokens are upper-cased to match the rest of the assembler.
    /// </summary>
    private static string[] ParseDbArgs(string originalLine, int lineNumber)
    {
        // Skip past the directive keyword.
        var keywordEnd = originalLine.IndexOf(' ');
        if (keywordEnd < 0)
            return Array.Empty<string>();

        var afterKeyword = originalLine.Substring(keywordEnd).Trim();
        var result = new List<string>();

        var i = 0;
        while (i < afterKeyword.Length)
        {
            var ch = afterKeyword[i];

            if (ch == ' ' || ch == ',')
            {
                i++;
                continue;
            }

            if (ch == '"')
            {
                // Quoted string literal — scan forward honouring \" escapes.
                var sb = new System.Text.StringBuilder();
                var j = i + 1;
                while (j < afterKeyword.Length && afterKeyword[j] != '"')
                {
                    if (
                        afterKeyword[j] == '\\'
                        && j + 1 < afterKeyword.Length
                        && afterKeyword[j + 1] == '"'
                    )
                    {
                        sb.Append('"'); // escaped quote → literal "
                        j += 2;
                    }
                    else
                    {
                        sb.Append(afterKeyword[j]);
                        j++;
                    }
                }
                if (j >= afterKeyword.Length)
                    throw new AssemblySyntaxException(
                        "Unterminated string literal in .DB directive",
                        lineNumber
                    );
                // Reconstruct as a quoted token the compiler can recognise.
                result.Add('"' + sb.ToString() + '"');
                i = j + 1; // skip past closing quote
            }
            else
            {
                // Plain token — upper-case it for symbol/number matching.
                var start = i;
                while (i < afterKeyword.Length && afterKeyword[i] != ',' && afterKeyword[i] != ' ')
                    i++;
                result.Add(afterKeyword.Substring(start, i - start).ToUpperInvariant());
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Counts how many bytes a .DB / .BYTES / .DATA directive will emit.
    /// Each quoted string literal expands to one byte per character;
    /// all other comma/space-separated tokens contribute one byte each.
    /// </summary>
    private static int CountDbBytes(string originalLine, int lineNumber)
    {
        // Find the directive keyword in the original line, then look at everything after it.
        var keywordEnd = originalLine.IndexOf(' ');
        if (keywordEnd < 0)
            return 0; // directive with no args

        var afterKeyword = originalLine.Substring(keywordEnd).Trim();

        // Walk through the argument list, counting:
        //   - string literals  → length of the string
        //   - everything else  → 1 byte each
        var total = 0;
        var i = 0;
        while (i < afterKeyword.Length)
        {
            var ch = afterKeyword[i];
            if (ch == '"')
            {
                // Scan forward honouring \" escapes; each logical char = one byte.
                var j = i + 1;
                while (j < afterKeyword.Length && afterKeyword[j] != '"')
                {
                    if (
                        afterKeyword[j] == '\\'
                        && j + 1 < afterKeyword.Length
                        && afterKeyword[j + 1] == '"'
                    )
                        j += 2; // escaped quote counts as one char
                    else
                        j++;
                    total++;
                }
                if (j >= afterKeyword.Length)
                    throw new AssemblySyntaxException(
                        "Unterminated string literal in .DB directive",
                        lineNumber
                    );
                i = j + 1; // skip past closing quote
            }
            else if (ch == ' ' || ch == ',')
            {
                i++;
            }
            else
            {
                // Non-string token: skip to next delimiter and count 1 byte.
                var next = i;
                while (
                    next < afterKeyword.Length
                    && afterKeyword[next] != ','
                    && afterKeyword[next] != ' '
                )
                    next++;
                if (next > i)
                    total++;
                i = next;
            }
        }

        return total;
    }

    private List<string> PreProcess(IEnumerable<string> lines, string currentDir)
    {
        var expandedLines = new List<string>();
        var lineNum = 0;

        foreach (var line in lines)
        {
            lineNum++;
            var trimmed = line.Trim();
            if (trimmed.StartsWith(".INCLUDE", StringComparison.OrdinalIgnoreCase))
            {
                var firstQuote = line.IndexOf('"');
                var lastQuote = line.LastIndexOf('"');

                if (firstQuote != -1 && lastQuote > firstQuote)
                {
                    var includeFileName = line.Substring(
                        firstQuote + 1,
                        lastQuote - firstQuote - 1
                    );

                    if (!includeFileName.EndsWith(".asm"))
                        throw new AssemblySyntaxException(
                            $"Could not include non-assembly file {includeFileName}",
                            lineNum
                        );

                    var fullPath = Path.Combine(currentDir, includeFileName);

                    if (File.Exists(fullPath))
                    {
                        // recursively process the included file so it can have includes too
                        var includedLines = File.ReadAllLines(fullPath);
                        expandedLines.AddRange(
                            PreProcess(includedLines, Path.GetDirectoryName(fullPath)!)
                        );
                        continue;
                    }
                    throw new AssemblySyntaxException($"Could not find '{fullPath}'", lineNum);
                }
            }
            expandedLines.Add(line);
        }
        expandedLines.Add("END-INCLUDE-ABCDEFGHIJKLMNOPQRSTUVWXYZ-BANANA"); // technically this means if you write that you trick the assembler. I don't care.
        return expandedLines;
    }
}
