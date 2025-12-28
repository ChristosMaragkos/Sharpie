using System.Text.RegularExpressions;

namespace Sharpie.Sdk.Asm;

public partial class Assembler
{
    private int CurrentAddress = 0;
    private static readonly char[] CommonDelimiters = [','];

    private IEnumerable<string>? FileContents { get; set; }
    public Dictionary<string, ushort> LabelToMemAddr { get; } = new();
    private Dictionary<string, ushort> Constants { get; } = new();
    public List<TokenLine> Tokens { get; } = new();

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
            cleanLine = line;
            RemoveComment(ref cleanLine);
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
            Tokens.Add(tokenLine);
        }

        Compile();
        return;
        bool IsLineEmpty(string line) => string.IsNullOrWhiteSpace(line);
    }

    private void ParseConstantDefinition(ref string line, int lineNumber)
    {
        if (!line.StartsWith(".DEF"))
            return;

        var constant = line.Remove(0, 4).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (constant.Length > 2)
            throw new AssemblySyntaxException($"Unexpected token: {constant.Last()}", lineNumber);
        if (constant.Length < 2)
            throw new AssemblySyntaxException(
                "Expected constant definition for directive .DEF",
                lineNumber
            );

        var (name, valueStr) = (constant[0], constant[1]);
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

        Constants[name] = (ushort)value;
        line = new string(
            line.Remove(0, (".DEF " + name + " " + valueStr).Length).ToArray()
        ).Trim();
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
        FileContents = File.ReadAllLines(filePath);
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
        if (line.Split(':').Length > 2)
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
        var args = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < args.Length; i++)
            args[i] = args[i].TrimEnd(CommonDelimiters).Trim();

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
                        CurrentAddress = ParseWord(args[1], lineNumber);

                    break;

                case ".SPRITE":
                    if (args.Length > 2)
                        throw new AssemblySyntaxException(
                            $"Unexpected token: {args.Last()}",
                            lineNumber
                        );
                    else if (args.Length < 2)
                        throw new AssemblySyntaxException(
                            "Directive .SPRITE expected a valid memory address",
                            lineNumber
                        );
                    else
                    {
                        var spriteIndex = ParseByte(args[1], lineNumber);

                        CurrentAddress = CalculateSpriteAddress(spriteIndex);
                    }

                    break;

                case ".DB":
                case ".BYTES":
                case ".DATA":
                    CurrentAddress += (args.Length - 1);
                    break;

                default:
                    throw new AssemblySyntaxException(
                        $"Unknown directive: {tokenLine.Opcode}",
                        lineNumber
                    );
            }
            return; // no need to check for an opcode
        }

        if (!InstructionSet.IsValidOpcode(tokenLine.Opcode))
            throw new AssemblySyntaxException($"Invalid Opcode: {tokenLine.Opcode}", lineNumber);

        if (args.Length - 1 != InstructionSet.GetOpcodeWords(tokenLine.Opcode))
            throw new AssemblySyntaxException(
                $"Invalid argument count for opcode {tokenLine.Opcode}: expected {InstructionSet.GetOpcodeWords(tokenLine.Opcode)} but found {args.Length}"
            );

        CurrentAddress += InstructionSet.GetOpcodeLength(tokenLine.Opcode);
    }
}
