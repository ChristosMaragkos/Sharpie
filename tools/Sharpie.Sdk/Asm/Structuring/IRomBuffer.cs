namespace Sharpie.Sdk.Asm.Structuring;

public interface IRomBuffer
{
    ushort Size { get; }
    ushort Cursor { get; }
    byte[] ByteBuffer { get; }
    bool[] TouchedBytes { get; }
    bool IsReadOnly { get; }
    List<TokenLine> Tokens { get; init; }
    Stack<ScopeLevel> Scopes { get; init; }
    Dictionary<int, ScopeLevel> AllScopes { get; init; }
    int ScopeCounter { get; set; }

    string Name { get; init; }

    public static readonly ScopeLevel GlobalScope = new(null, 0);

    void MakeReadOnly();
    void AdvanceCursor();
    public void WriteByte(byte value)
    {
        if (Cursor >= ByteBuffer.Length)
            throw new SharpieRomSizeException(
                $"Could not write to {Name} - it would exceed the maximum size of {Size} by {Cursor - Size} bytes."
            );
        if (IsReadOnly)
            throw new SharpieRomSizeException(
                $"Could not write to {Name} - sections are made read-only after .ENDREGION"
            );
        if (TouchedBytes[Cursor])
            throw new SharpieRomSizeException(
                $"Could not write to {Name} - there has already been a write at address {Cursor} ({Cursor:X4})"
            );
        ByteBuffer[Cursor] = value;
        TouchedBytes[Cursor] = true;
        AdvanceCursor();
    }

    public void WriteWord(ushort value)
    {
        var low = (byte)value;
        var high = (byte)(value >> 8);
        WriteByte(low);
        WriteByte(high);
    }

    public void NewScope(ScopeLevel? parent)
    {
        var scope = new ScopeLevel(parent, ScopeCounter);
        Scopes.Push(scope);
        AllScopes[ScopeCounter] = scope;
        ScopeCounter++;
    }

    public void ExitScope()
    {
        if (Scopes.Count <= 2)
            throw new AssemblySyntaxException($"Cannot exit global scope of region {Name}");
    }

    public ScopeLevel CurrentScope => Scopes.Peek();
    public ScopeLevel ScopeById => AllScopes[CurrentScope!.Id];
}

public class FixedRegionBuffer : IRomBuffer
{
    public ushort Size { get; } = 18 * 1024;
    public ushort Cursor { get; private set; }
    public byte[] ByteBuffer { get; }
    public bool[] TouchedBytes { get; }
    public bool IsReadOnly { get; private set; }
    public string Name { get; init; } = "Fixed Region";
    public List<TokenLine> Tokens { get; init; } = new();
    public Stack<ScopeLevel> Scopes { get; init; } = new();
    public Dictionary<int, ScopeLevel> AllScopes { get; init; } = new();
    public int ScopeCounter { get; set; } = 1;

    public FixedRegionBuffer()
    {
        ByteBuffer = new byte[Size];
        TouchedBytes = new bool[Size];
        IsReadOnly = false;
        Scopes.Push(IRomBuffer.GlobalScope);
        (this as IRomBuffer).NewScope(null);
    }

    public void MakeReadOnly()
    {
        IsReadOnly = true;
    }

    public void AdvanceCursor()
    {
        Cursor++;
    }
}

public class BankBuffer : IRomBuffer
{
    public ushort Size { get; } = 32 * 1024;
    public ushort Cursor { get; private set; }
    public byte[] ByteBuffer { get; }
    public bool[] TouchedBytes { get; }
    public bool IsReadOnly { get; private set; }
    public static int TotalBanksCreated = 0;
    public string Name { get; init; }
    public List<TokenLine> Tokens { get; init; } = new();
    public Stack<ScopeLevel> Scopes { get; init; } = new();
    public Dictionary<int, ScopeLevel> AllScopes { get; init; } = new();
    public int ScopeCounter { get; set; } = 1;

    public BankBuffer()
    {
        ByteBuffer = new byte[Size];
        TouchedBytes = new bool[Size];
        IsReadOnly = false;
        Name = $"Bank {TotalBanksCreated}";
        TotalBanksCreated++;
        Scopes.Push(IRomBuffer.GlobalScope);
        (this as IRomBuffer).NewScope(null);
    }

    public void MakeReadOnly()
    {
        IsReadOnly = true;
    }

    public void AdvanceCursor()
    {
        Cursor++;
    }
}

public class SpriteAtlasBuffer : IRomBuffer
{
    public ushort Size { get; } = 8 * 1024;
    public ushort Cursor { get; set; }
    public byte[] ByteBuffer { get; }
    public bool[] TouchedBytes { get; }
    public bool IsReadOnly { get; set; }
    public string Name { get; init; } = "Sprite Atlas";
    public List<TokenLine> Tokens { get; init; } = new();
    public Stack<ScopeLevel> Scopes { get; init; } = new();
    public Dictionary<int, ScopeLevel> AllScopes { get; init; } = new();
    public int ScopeCounter { get; set; } = 1;

    public SpriteAtlasBuffer()
    {
        ByteBuffer = new byte[Size];
        TouchedBytes = new bool[Size];
        IsReadOnly = false;
        Scopes.Push(IRomBuffer.GlobalScope);
        (this as IRomBuffer).NewScope(null);
    }

    public void MakeReadOnly()
    {
        IsReadOnly = true;
    }

    public void AdvanceCursor()
    {
        Cursor++;
    }

    public void PositionCursor(int spriteIndex)
    {
        if (spriteIndex >= 256 || spriteIndex < 0)
            throw new AssemblySyntaxException(
                $"Sprite index {spriteIndex} is out of the [0-255] range."
            );

        Cursor = (ushort)(Size - 32 * spriteIndex);
    }
}
