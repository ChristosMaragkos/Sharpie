using SpriteFlags = Sharpie.Core.Hardware.OamBank.SpriteFlags;

namespace Sharpie.Core.Hardware;

internal partial class Ppu
{
    private const int DisplayHeight = 256;
    private const int DisplayWidth = 256;
    private const int WorldHeight = ushort.MaxValue;
    private const int WorldWidth = ushort.MaxValue;

    private const ushort SpriteMemoryStart = Memory.SpriteAtlasStart;

    private int _currentBuffer = 0;

    private ushort DisplayStart => (ushort)(_currentBuffer == 0 ? 0x0000 : 0x8000);
    private ushort RenderStart => (ushort)(_currentBuffer == 0 ? 0x8000 : 0x0000);

    private readonly IMotherboard _mobo;
    private readonly Memory _vRam;

    private readonly byte[] _spriteBuffer = new byte[64]; // here so the GC doesn't cry

    private readonly (byte X, byte Y, byte TileId, byte Attr)[] _hudSprites = new (
        byte,
        byte,
        byte,
        byte
    )[OamBank.MaxHudEntries];

    private int _totalHudEntries = 0;

    public ushort CamX
    {
        get;
        set { field = Math.Clamp(value, (ushort)0, (ushort)(WorldWidth - DisplayWidth)); }
    } = 0;

    public ushort CamY
    {
        get;
        set { field = Math.Clamp(value, (ushort)0, (ushort)(WorldHeight - DisplayHeight)); }
    } = 0;

    public byte BackgroundColorIndex { get; set; } = 0;

    public Ppu(IMotherboard mobo)
    {
        _mobo = mobo;
        _vRam = new Memory();
    }

    private void DecodeSprite(byte index, byte attributes)
    {
        var flipH = (attributes & (byte)SpriteFlags.FlipH) != 0;
        var flipV = (attributes & (byte)SpriteFlags.FlipV) != 0;

        var spriteStartAddr = SpriteMemoryStart - (32 * (index + 1));
        for (int row = 0; row < 8; row++)
        {
            var realRow = flipV ? (7 - row) : row;
            for (int column = 0; column < 4; column++) // 4 bytes per row because of indexed color
            {
                var packed = _mobo.ReadByte(spriteStartAddr + (row * 4) + column);
                var pixel1 = (byte)((packed >> 4) & 0x0F);
                var pixel2 = (byte)(packed & 0x0F);

                var realColumn1 = flipH ? (7 - column * 2) : (column * 2); // pemdas amirite
                var realColumn2 = flipH ? (7 - (column * 2 + 1)) : (column * 2 + 1);
                _spriteBuffer[realRow * 8 + realColumn1] = pixel1;
                _spriteBuffer[realRow * 8 + realColumn2] = pixel2;
            }
        }
    }

    public void FlipBuffers() => _currentBuffer = 1 - _currentBuffer;

    private void WritePixel(int x, int y, byte colorIndex)
    {
        if (x < 0 || x >= DisplayWidth || y < 0 || y >= DisplayHeight)
            return;
        if (colorIndex == 0)
            return;

        var pixelIndex = y * 256 + x;
        var byteOffset = pixelIndex / 2;
        var isHighNibble = (pixelIndex & 1) == 0;

        var existingPixel = _vRam.ReadByte(RenderStart + byteOffset);
        if (isHighNibble)
            existingPixel = (byte)((existingPixel & 0x0F) | (colorIndex << 4));
        else
            existingPixel = (byte)((existingPixel & 0xF0) | (colorIndex & 0x0F));

        _vRam.WriteByte(RenderStart + byteOffset, existingPixel);
    }

    public void VBlank(OamBank oam)
    {
        _totalHudEntries = 0;
        FillBuffer(BackgroundColorIndex);

        ProcessOam(oam);
        ProcessHud();
        ProcessText();
    }

    private void ProcessOam(OamBank oam)
    {
        for (var oamIndex = 0; oamIndex < OamBank.MaxEntries; oamIndex++)
        {
            var (x, y, spriteId, attributes, type) = oam.ReadEntry(oamIndex);

            if (
                x == 0xFFFF
                && y == 0xFFFF
                && spriteId == 0xFF
                && attributes == 0xFF
                && type == 0xFF
            )
                continue;

            if ((attributes & (byte)SpriteFlags.Hud) != 0)
            {
                if (x > byte.MaxValue || y > byte.MaxValue)
                    continue; // HUD sprite is off-screen, don't even bother

                _hudSprites[_totalHudEntries % OamBank.MaxHudEntries] = (
                    (byte)x,
                    (byte)y,
                    spriteId,
                    attributes
                );
                _totalHudEntries++;
                continue;
            }

            var localX = x - CamX;
            var localY = y - CamY;

            if (
                localX + 8 <= 0
                || localY + 8 <= 0
                || localX >= DisplayWidth
                || localY >= DisplayHeight
            )
                continue; // sprite is fully outside camera

            DecodeSprite(spriteId, attributes);

            var startX = Math.Max(0, -localX);
            var endX = Math.Min(8, DisplayWidth - localX);

            var startY = Math.Max(0, -localY);
            var endY = Math.Min(8, DisplayHeight - localY);

            BlitSprite(localX, localY, startX, endX, startY, endY);
        }
    }

    private void ProcessHud()
    {
        for (var hudIndex = 0; hudIndex < _totalHudEntries; hudIndex++)
        {
            var (x, y, id, attr) = _hudSprites[hudIndex];
            DecodeSprite(id, attr);
            BlitSprite(x, y);
        }
    }

    private void ProcessText()
    {
        var textColor = _mobo.FontColorIndex;
        for (var charX = 0; charX < 32; charX++)
        {
            for (var charY = 0; charY < 32; charY++)
            {
                var charIndex = _mobo.TextGrid[charX, charY];
                if (charIndex == 0xFF)
                    continue;
                var charSprite = IMotherboard.GetCharacter(charIndex);
                BlitCharacter(charX << 3, charY << 3, textColor, charSprite); // multiply x and y by 8 to get real screen coords
            }
        }
    }

    private void BlitSprite(
        int x,
        int y,
        int startX = 0,
        int endX = 8,
        int startY = 0,
        int endY = 8
    )
    {
        for (int row = startY; row < endY; row++)
        for (int column = startX; column < endX; column++)
        {
            WritePixel(x + column, y + row, _spriteBuffer[row * 8 + column]);
        }
    }

    public void BlitCharacter(int x, int y, byte colorIndex, byte[] pixels)
    {
        for (var i = 0; i < pixels.Length; i++)
        {
            byte rowData = pixels[i];
            for (var bit = 0; bit < 8; bit++)
            {
                var isPixelSet = ((rowData << bit) & 0x80) != 0;
                if (isPixelSet)
                    WritePixel(x + bit, y + i, colorIndex);
            }
        }
    }

    private void FillBuffer(byte colorIndex)
    {
        Span<byte> vramSpan = _vRam.Slice(RenderStart, 32768);
        vramSpan.Fill((byte)((colorIndex << 4) | colorIndex));
    }

    [Obsolete(
        "Dumping VRAM into the console like this evidently is a performance nuke. Shocker, I know.",
        true
    )]
    public void DumpVram(ushort start, int width, int height)
    {
        Console.WriteLine($"--- VRAM DUMP AT {start:X4} ---");
        for (int y = 0; y < height; y++)
        {
            var line = $"{y:D2}: ";
            for (int x = 0; x < width; x++)
            {
                byte val = _vRam.ReadByte(RenderStart + start + (y * 128) + x);
                line += $"{val:X2} ";
            }
            Console.WriteLine(line);
        }
        Console.WriteLine("-----------------------------------");
    }
}
