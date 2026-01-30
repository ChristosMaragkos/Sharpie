namespace Sharpie.Sdk.Asm.Structuring;

public interface IRomBuffer
{
    int Size { get; }
    ushort Cursor { get; }
    byte[] ByteBuffer { get; }
    bool[] TouchedBytes { get; }
    List<TokenLine> Tokens { get; init; }
    Stack<ScopeLevel> Scopes { get; init; }
    Dictionary<int, ScopeLevel> AllScopes { get; init; }
    int ScopeCounter { get; set; }

    string Name { get; init; }

    public static readonly ScopeLevel GlobalScope = new(null, 0);

    void AdvanceCursor(int amount = 1);
    void SetCursor(int address);
    public void WriteByte(byte value)
    {
        if (Cursor >= ByteBuffer.Length)
            throw new SharpieRomSizeException(
                $"Could not write to {Name} - it would exceed the maximum size of {Size} by {Cursor - Size} bytes."
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

    public void NewScope()
    {
        var scope = new ScopeLevel(CurrentScope, ScopeCounter);
        Scopes.Push(scope);
        AllScopes[ScopeCounter] = scope;
        ScopeCounter++;
    }

    public void ExitScope()
    {
        if (Scopes.Count <= 2)
            throw new AssemblySyntaxException($"Cannot exit global scope of region {Name}");
        Scopes.Pop();
    }

    public ScopeLevel CurrentScope => Scopes.Peek();
    public ScopeLevel ScopeById => AllScopes[CurrentScope!.Id];
}

public class FixedRegionBuffer : IRomBuffer
{
    public int Size { get; } = 18 * 1024;
    public ushort Cursor { get; private set; }
    public byte[] ByteBuffer { get; }
    public bool[] TouchedBytes { get; }
    public string Name { get; init; } = "Fixed Region";
    public List<TokenLine> Tokens { get; init; } = new();
    public Stack<ScopeLevel> Scopes { get; init; } = new();
    public Dictionary<int, ScopeLevel> AllScopes { get; init; } = new();
    public int ScopeCounter { get; set; } = 1;

    public FixedRegionBuffer()
    {
        ByteBuffer = new byte[Size];
        TouchedBytes = new bool[Size];
        Scopes.Push(IRomBuffer.GlobalScope);
        (this as IRomBuffer).NewScope();
    }

    public void AdvanceCursor(int amount = 1)
    {
        Cursor += (ushort)amount;
    }

    public void SetCursor(int address)
    {
        Cursor = (ushort)address;
    }
}

public class BankBuffer : IRomBuffer
{
    public int Size { get; } = 32 * 1024;
    public ushort Cursor { get; private set; }
    public byte[] ByteBuffer { get; }
    public bool[] TouchedBytes { get; }
    public static int TotalBanksCreated = 0;
    public string Name { get; init; }
    public List<TokenLine> Tokens { get; init; } = new();
    public Stack<ScopeLevel> Scopes { get; init; } = new();
    public Dictionary<int, ScopeLevel> AllScopes { get; init; } = new();
    public int ScopeCounter { get; set; } = 1;
    public int BankId { get; init; }

    public BankBuffer()
    {
        ByteBuffer = new byte[Size];
        TouchedBytes = new bool[Size];
        Name = $"Bank {TotalBanksCreated}";
        BankId = TotalBanksCreated++;
        Scopes.Push(IRomBuffer.GlobalScope);
        (this as IRomBuffer).NewScope();
    }

    public void AdvanceCursor(int amount = 1)
    {
        Cursor += (ushort)amount;
    }

    public void SetCursor(int address)
    {
        Cursor = (ushort)address;
    }
}

public abstract class SpriteCapableBuffer : IRomBuffer
{
    public abstract int Size { get; }
    public abstract ushort Cursor { get; protected set; }
    public abstract byte[] ByteBuffer { get; }
    public abstract bool[] TouchedBytes { get; }
    public abstract List<TokenLine> Tokens { get; init; }
    public abstract Stack<ScopeLevel> Scopes { get; init; }
    public abstract Dictionary<int, ScopeLevel> AllScopes { get; init; }
    public abstract int ScopeCounter { get; set; }
    public abstract string Name { get; init; }

    public abstract void AdvanceCursor(int amount = 1);

    public virtual void PositionCursor(int spriteIndex)
    {
        if (spriteIndex >= 256 || spriteIndex < 0)
            throw new AssemblySyntaxException(
                $"Sprite index {spriteIndex} is out of the [0-255] range."
            );
    }

    public void SetCursor(int address)
    {
        Cursor = (ushort)address;
    }
}

public class SpriteAtlasBuffer : SpriteCapableBuffer
{
    public override int Size { get; } = 8 * 1024;
    public override ushort Cursor { get; protected set; }
    public override byte[] ByteBuffer { get; }
    public override bool[] TouchedBytes { get; }
    public override string Name { get; init; } = "Sprite Atlas";
    public override List<TokenLine> Tokens { get; init; } = new();
    public override Stack<ScopeLevel> Scopes { get; init; } = new();
    public override Dictionary<int, ScopeLevel> AllScopes { get; init; } = new();
    public override int ScopeCounter { get; set; } = 1;

    public SpriteAtlasBuffer()
    {
        ByteBuffer = new byte[Size];
        TouchedBytes = new bool[Size];
        Scopes.Push(IRomBuffer.GlobalScope);
        (this as IRomBuffer).NewScope();
    }

    public override void AdvanceCursor(int amount = 1)
    {
        Cursor += (ushort)amount;
    }

    public override void PositionCursor(int spriteIndex)
    {
        base.PositionCursor(spriteIndex);
        var LastIndex = Size - 1;
        Cursor = (ushort)(LastIndex - 32 * (spriteIndex + 1));
    }
}

public class FirmwareBuffer : SpriteCapableBuffer
{
    public FirmwareBuffer()
    {
        ByteBuffer = new byte[Size];
        TouchedBytes = new bool[Size];
        Scopes.Push(IRomBuffer.GlobalScope);
        (this as IRomBuffer).NewScope();
    }

    public override int Size { get; } = 64 * 1024;
    public override ushort Cursor { get; protected set; }
    public override byte[] ByteBuffer { get; }
    public override bool[] TouchedBytes { get; }
    public override List<TokenLine> Tokens { get; init; } = new();
    public override Stack<ScopeLevel> Scopes { get; init; } = new();
    public override Dictionary<int, ScopeLevel> AllScopes { get; init; } = new();
    public override int ScopeCounter { get; set; } = 1;
    public override string Name { get; init; } = "Full ROM Buffer";

    public override void AdvanceCursor(int amount = 1)
    {
        Cursor += (ushort)amount;
    }

    public override void PositionCursor(int spriteIndex)
    {
        base.PositionCursor(spriteIndex);
        var LastIndex = (58 * 1024) - 1; // The last index of the maximum addressable cartridge ram space
        Cursor = (ushort)(LastIndex - 32 * (spriteIndex + 1));
    }
}
