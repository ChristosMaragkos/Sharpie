using System.Text.RegularExpressions;

namespace Sharpie.Sdk.Asm;

public partial class Assembler
{
    private int CurrentAddress = 0;
    private static readonly char[] CommonDelimiters = [',', ' '];

    private IEnumerable<string>? FileContents { get; set; }
    public List<TokenLine> Tokens { get; } = new();

    private bool _isInAssetMode;
    private int _realCursor;

    private static readonly char[] DisallowedEnumChars = [':', ',', '#', '=', ' ', '\'', '"'];

    private string? _currentEnum = null;
    private ushort _currentEnumVal;

    private readonly Stack<ScopeLevel> _scopes = new();

    private void NewScope() => _scopes.Push(new ScopeLevel());

    private void RemoveScope()
    {
        if (!IsInLocalScope)
            throw new InvalidOperationException("Cannot remove global scope.");

        _scopes.Pop();
    }

    private ScopeLevel CurrentScope => _scopes.Peek();
    private bool IsInLocalScope => _scopes.Count > 1;

    private void ReadFile()
    {
        _scopes.Push(new ScopeLevel()); // global scope

        if (FileContents == null)
            throw new NullReferenceException("File contents are null. Check your file path.");

        Console.WriteLine("Assembler: Reading file...");

        var lineNum = 0;
        string cleanLine;
        foreach (var line in FileContents!)
        {
            var tokenLine = new TokenLine();
            lineNum++;
            tokenLine.SourceLine = lineNum;
            tokenLine.Address = CurrentAddress;
            cleanLine = line.Trim().ToUpper();
            RemoveComment(ref cleanLine);
            if (IsLineEmpty(cleanLine))
                continue;

            if (line == "END-INCLUDE-ABCDEFGHIJKLMNOPQRSTUVWXYZ-BANANA")
            {
                lineNum = 0;
                continue;
            }

            var isAssetDirective =
                cleanLine.StartsWith(".SPRITE")
                || cleanLine.StartsWith(".DB")
                || cleanLine.StartsWith(".DATA")
                || cleanLine.StartsWith(".BYTES")
                || cleanLine.StartsWith(".DW");

            if (_isInAssetMode && !isAssetDirective)
            {
                CurrentAddress = _realCursor;
                _isInAssetMode = false;
            }

            ParseScope(ref cleanLine, lineNum);
            if (IsLineEmpty(cleanLine))
                continue;

            ParseEnumDefinition(ref cleanLine, lineNum);
            if (IsLineEmpty(cleanLine))
                continue;

            ParseConstantDefinition(ref cleanLine, lineNum);
            if (IsLineEmpty(cleanLine)) // should be empty but oh well
                continue;

            ParseStringDirective(ref cleanLine, lineNum);
            if (IsLineEmpty(cleanLine))
                continue;

            RemoveLabel(ref cleanLine, lineNum);
            if (IsLineEmpty(cleanLine))
                continue;

            Tokenize(cleanLine, ref tokenLine, lineNum);

            if (tokenLine.ArePropertiesNull())
                continue;
            if (tokenLine.Opcode != "ALT")
                Tokens.Add(tokenLine); // ALT is added right when tokenizing
        }

        Compile();
        return;

        bool IsLineEmpty(string line) => string.IsNullOrWhiteSpace(line);
    }

    private void ParseScope(ref string cleanLine, int lineNum)
    {
        if (cleanLine.StartsWith(".SCOPE"))
        {
            NewScope();
            cleanLine = cleanLine.Substring(".SCOPE".Length).Trim(); // allow labels and such after scope start
        }
        else if (cleanLine.StartsWith(".ENDSCOPE"))
        {
            if (!IsInLocalScope)
                throw new AssemblySyntaxException(
                    "No matching .SCOPE found for .ENDSCOPE",
                    lineNum
                );

            RemoveScope();
            cleanLine = cleanLine.Substring(".ENDSCOPE".Length).Trim();
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
                || !TryDefineEnum(name)
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

                if (!TryDefineEnumMember(_currentEnum, enumMember, _currentEnumVal++))
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

                if (!TryDefineEnumMember(_currentEnum, enumMember, value))
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

        for (int i = 0; i < args.Length; i++)
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
            || !TryDefineConstant(name, (ushort)value)
            || TryResolveEnum(name)
        )
            throw new AssemblySyntaxException($"Constant {name} is already declared", lineNumber);

        if (value > ushort.MaxValue)
            throw new AssemblySyntaxException(
                $"Number {value} cannot be larger than {ushort.MaxValue}.",
                lineNumber
            );

        line = line.Substring(line.LastIndexOf(valueStr.Last()) + 1);
    }

    private void ParseStringDirective(ref string line, int lineNumber)
    {
        if (!line.StartsWith(".STR"))
            return;

        int firstQuote = line.IndexOf('"');
        int lastQuote = line.LastIndexOf('"');

        if (firstQuote == -1 || lastQuote == -1 || firstQuote == lastQuote)
            throw new AssemblySyntaxException(
                "String literal must be wrapped in double quotes",
                lineNumber
            );

        string message = line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
        string coordPart = line.Substring(4, firstQuote - 4).Trim();
        var coordArgs = coordPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (coordArgs.Length != 2)
            throw new AssemblySyntaxException(
                "Expected X and Y coordinates before the string",
                lineNumber
            );
        for (int i = 0; i < coordArgs.Length; i++)
            coordArgs[i] = coordArgs[i].Trim(CommonDelimiters);

        Tokens.Add(
            new TokenLine
            {
                Opcode = "SETCRS",
                Args = new[] { coordArgs[0], coordArgs[1] },
                SourceLine = lineNumber,
                Address = CurrentAddress,
            }
        );
        CurrentAddress += InstructionSet.GetOpcodeLength("SETCRS");

        var delta = InstructionSet.GetOpcodeLength("TEXT");
        foreach (char c in message)
        {
            TokenLine tl = new()
            {
                Opcode = "TEXT",
                Args = new[] { TextHelper.GetFontIndex(c).ToString() },
                SourceLine = lineNumber,
                Address = CurrentAddress,
            };
            Tokens.Add(tl);
            CurrentAddress += delta;
        }

        var remainder = line.Substring(lastQuote + 1).TrimStart(CommonDelimiters).Trim();
        var extraArgs = remainder.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var arg in extraArgs)
        {
            var cleanArg = arg.Trim(CommonDelimiters).Trim();
            var value = ParseRegister(cleanArg, lineNumber);
            Tokens.Add(
                new()
                {
                    Opcode = "ALT",
                    Args = Array.Empty<string>(),
                    SourceLine = lineNumber,
                    Address = CurrentAddress,
                }
            );
            CurrentAddress += InstructionSet.GetOpcodeLength("ALT");

            Tokens.Add(
                new()
                {
                    Opcode = "TEXT",
                    Args = new[] { cleanArg },
                    SourceLine = lineNumber,
                    Address = CurrentAddress,
                }
            );
            CurrentAddress += InstructionSet.GetOpcodeLength("TEXT");
        }

        line = string.Empty;
    }

    public void LoadFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"No file by name \"{filePath}\" was found.");
        if (!filePath.EndsWith(".asm"))
            throw new Exception($"File \"{filePath}\" is not in the \".asm\" format.");
        Console.WriteLine("Assembler: Loading file...");
        var initialFile = File.ReadAllLines(filePath);
        FileContents = PreProcess(initialFile, Path.GetDirectoryName(filePath)!);
        ReadFile();
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
            !TryDefineLabel(label, (ushort)CurrentAddress)
            || TryResolveConstant(label, out _)
            || TryResolveEnum(label)
        )
            throw new AssemblySyntaxException($"Label {label} is already declared", lineNumber);

        line = line.Substring(labelRegex.Index + labelRegex.Length).Trim();
    }

    private void Tokenize(string line, ref TokenLine tokenLine, int lineNumber)
    {
        var args = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(str => str.Trim(CommonDelimiters))
            .Where(str => !string.IsNullOrWhiteSpace(str))
            .ToArray();

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
                        CurrentAddress = ParseWord(args[1], lineNumber);
                        tokenLine.Address = CurrentAddress;
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
                        if (!_isInAssetMode)
                        {
                            _realCursor = CurrentAddress;
                            _isInAssetMode = true;
                        }

                        var spriteIndex = ParseByte(args[1], lineNumber);
                        CurrentAddress = CalculateSpriteAddress(spriteIndex);
                        tokenLine.Address = CurrentAddress;
                    }

                    break;

                case ".DB":
                case ".BYTES":
                case ".DATA":
                    tokenLine.Address = CurrentAddress;
                    CurrentAddress += (args.Length - 1);
                    break;

                case ".DW":
                    tokenLine.Address = CurrentAddress;
                    CurrentAddress += (2 * (args.Length - 1));
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
            Tokens.Add(
                new TokenLine
                {
                    Opcode = "ALT",
                    Args = Array.Empty<string>(),
                    SourceLine = lineNumber,
                    Address = CurrentAddress,
                }
            );
            CurrentAddress += InstructionSet.GetOpcodeLength("ALT");

            var remainingLine = string.Join(' ', args.Skip(1));
            var nextToken = new TokenLine
            {
                SourceLine = lineNumber,
                Address = CurrentAddress,
                // Args = args.Skip(1).ToArray(),
            };
            Tokenize(remainingLine, ref nextToken, lineNumber);
            Tokens.Add(nextToken);
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
            tokenLine.Address = CurrentAddress;

        CurrentAddress += InstructionSet.GetOpcodeLength(tokenLine.Opcode);
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
                int firstQuote = line.IndexOf('"');
                int lastQuote = line.LastIndexOf('"');

                if (firstQuote != -1 && lastQuote > firstQuote)
                {
                    string includeFileName = line.Substring(
                        firstQuote + 1,
                        lastQuote - firstQuote - 1
                    );

                    if (!includeFileName.EndsWith(".asm"))
                        throw new AssemblySyntaxException(
                            $"Could not include non-assembly file {includeFileName}",
                            lineNum
                        );

                    string fullPath = Path.Combine(currentDir, includeFileName);

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
