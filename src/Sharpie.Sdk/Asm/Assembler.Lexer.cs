using System.Text.RegularExpressions;

namespace Sharpie.Sdk.Asm;

public partial class Assembler
{
    private int CurrentAddress = 0;
    private static readonly char[] CommonDelimiters = [','];

    private IEnumerable<string>? FileContents { get; set; }
    public Dictionary<string, ushort> LabelToMemAddr { get; } = new();
    private Dictionary<string, ushort> Constants { get; } = new();
    private Regex Regex { get; } = new("");
    public List<TokenLine> Tokens { get; } = new();

    public void ReadFile()
    {
        if (FileContents == null)
            throw new NullReferenceException("File contents are null. Check your file path.");

        var lineNum = 0;
        string cleanLine;
        TokenLine tokenLine;
        foreach (var line in FileContents!)
        {
            tokenLine = new TokenLine();
            lineNum++;
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
        if (!IsValidInteger(valueStr, out var value, out bool _))
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

        int x = 0;
        int y = 0;
        ConstantOrParse(coordArgs[0].Trim(CommonDelimiters), ref x);
        AssertSize(x);
        ConstantOrParse(coordArgs[1].Trim(CommonDelimiters), ref y);
        AssertSize(y);

        TokenLine tl;
        foreach (char c in message)
        {
            tl = new();
            tl.Args = new string[3];
            tl.Opcode = "TEXT";
            tl.Args[0] = x.ToString();
            tl.Args[1] = y.ToString();
            tl.Args[2] = TextHelper.GetFontIndex(c).ToString();
            x++;
            Tokens.Add(tl);
            CurrentAddress += InstructionSet.GetOpcodeLength("TEXT")!.Value;
        }

        line = line.Remove(0, lastQuote + 1);
        return;

        void ConstantOrParse(string numStr, ref int result)
        {
            if (Constants.TryGetValue(numStr, out var constant))
                result = constant;
            else if (IsValidInteger(numStr, out var parsed, out bool _))
                result = parsed!.Value;
            else
                throw new AssemblySyntaxException(
                    $"Token {numStr} is not a valid number",
                    lineNumber
                );
        }

        void AssertSize(int number)
        {
            if (number < 0 || number >= 32)
                throw new AssemblySyntaxException(
                    $"Grid index {x} must be no less than 0 and no more than 31",
                    lineNumber
                );
        }
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
            LabelToMemAddr[label] = (ushort)CurrentAddress;
            line = line.Substring(colonIndex + 1).Trim();
        }
    }

    private void Tokenize(string line, ref TokenLine tokenLine, int lineNumber)
    {
        var args = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var opcode = args[0];
        for (var i = 0; i < args.Length; i++)
            args[i] = args[i].TrimEnd(CommonDelimiters).Trim();

        if (!InstructionSet.IsValidOpcode(opcode))
            throw new AssemblySyntaxException($"Invalid Opcode: {opcode}", lineNumber);

        if (args.Length - 1 != InstructionSet.GetOpcodeWords(opcode))
            throw new AssemblySyntaxException(
                $"Invalid argument count for opcode {opcode}: expected {InstructionSet.GetOpcodeWords(opcode)} but found {args.Length}"
            );

        tokenLine.Opcode = opcode;
        tokenLine.Args = args.Skip(1).ToArray();
        CurrentAddress += InstructionSet.GetOpcodeLength(opcode)!.Value;
    }
}
