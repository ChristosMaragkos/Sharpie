namespace Sharpie.Sdk.Asm;

public class AssemblySyntaxException : Exception
{
    public AssemblySyntaxException() { }

    public AssemblySyntaxException(string message)
        : base(message) { }

    public AssemblySyntaxException(string message, int lineNumber)
        : this($"Syntax Error at line {lineNumber}: {message}") { }

    public AssemblySyntaxException(string message, System.Exception inner)
        : base(message, inner) { }
}

public class SharpieRomSizeException : Exception
{
    public SharpieRomSizeException() { }

    public SharpieRomSizeException(string message)
        : base(message) { }

    public SharpieRomSizeException(int lastAddr)
        : this(
            $"Exceeded maximum Sharpie Rom size of 48 kilobytes by {lastAddr - 49152} bytes. Optimize your code."
        ) { }
}
