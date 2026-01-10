using System.Text.RegularExpressions;

namespace Sharpie.Sdk.Asm;

public partial class Assembler
{
    private int CurrentAddress = 0;
    private static readonly char[] CommonDelimiters = [',', ' '];

    private IEnumerable<string>? FileContents { get; set; }
    public Dictionary<string, ushort> LabelToMemAddr { get; } = new();
    private Dictionary<string, ushort> Constants { get; } = new();
    public List<TokenLine> Tokens { get; } = new();
    private bool _isInAssetMode;
    private int _realCursor;

    // TODO: Stop tokenizing OUT instructions by distinguishing between Debug and Release ROMs
    private void ReadFile()
    {
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
        if (LabelToMemAddr.ContainsKey(name) || Constants.ContainsKey(name))
            throw new AssemblySyntaxException($"Constant {name} is already declared", lineNumber);

        var value = ParseNumberLiteral(valueStr, true);
        if (value == null)
            throw new AssemblySyntaxException(
                $"Unexpected token: {valueStr} - expected a number",
                lineNumber
            );
        if (value > ushort.MaxValue || value < 0 || value == null)
            throw new AssemblySyntaxException(
                $"Number {value} cannot be larger than {ushort.MaxValue} or smaller than zero",
                lineNumber
            );

        line = line.Substring(line.LastIndexOf(valueStr.Last()) + 1);

        Constants[name] = (ushort)value;
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

        line = line.Remove(0, lastQuote + 1);
    }

    /// Mostly used for unit tests
    public void ReadRawAssembly(string assemblyCode)
    {
        FileContents = assemblyCode.Split('\n');
        ReadFile();
        // foreach (var tl in Tokens)
        //     Console.WriteLine(tl.ToString());
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
            line = line.Remove(comment.Index);
            line = line.Trim();
        }
    }

    private void RemoveLabel(ref string line, int lineNumber)
    {
        if (line.Split(':', StringSplitOptions.RemoveEmptyEntries).Length > 2)
            throw new AssemblySyntaxException("Unexpected token: \":\"", lineNumber);

        var labelRegex = Regex.Match(line, ":");
        if (labelRegex.Success)
        {
            var colonIndex = labelRegex.Index;
            var label = line.Substring(0, colonIndex).Trim();

            if (Constants.ContainsKey(label) || LabelToMemAddr.ContainsKey(label))
                throw new AssemblySyntaxException($"Label {label} is already declared", lineNumber);

            LabelToMemAddr[label] = (ushort)CurrentAddress;
            line = line.Substring(colonIndex + 1).Trim();
        }
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

        if (args.Length - 1 != InstructionSet.GetOpcodeWords(tokenLine.Opcode))
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
        return expandedLines;
    }
}
