using SpriteFlags = Sharpie.Core.Hardware.OamBank.SpriteFlags;

namespace Sharpie.Core.Hardware;

internal enum BlitterMode
{
    Default = 0,
    NoText,
    NoOam,
    None
}

internal partial class Ppu
{
    private const int DisplayHeight = 256;
    private const int DisplayWidth = 256;
    private const int FrameSize = DisplayWidth * DisplayHeight;

    private const int WorldHeight = ushort.MaxValue + 1;
    private const int WorldWidth = ushort.MaxValue + 1;

    private const ushort SpriteMemoryStart = Memory.SpriteAtlasStart;

    private readonly IMotherboard _mobo;
    private readonly Memory _vRam;

    public byte ReadByte(ushort address) => _vRam.ReadByte(address);
    public void WriteByte(ushort address, byte value) => _vRam.WriteByte(address, value);

    private readonly byte[] _spriteBuffer = new byte[64]; // here so the GC doesn't cry

    public BlitterMode Mode = BlitterMode.Default;

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
        var colorOffset = (attributes & (byte)SpriteFlags.AlternatePalette) != 0 ? 16 : 0;

        var spriteStartAddr = SpriteMemoryStart - (32 * (index + 1));
        for (int row = 0; row < 8; row++)
        {
            var realRow = flipV ? (7 - row) : row;
            for (int column = 0; column < 4; column++) // 4 bytes per row because of indexed color
            {
                var packed = _mobo.ReadByte(spriteStartAddr + (row * 4) + column);
                var pixel1 = (byte)((packed >> 4) & 0x0F);
                var pixel2 = (byte)(packed & 0x0F);

                var realColumn1 = flipH ? (7 - (column * 2)) : (column * 2); // pemdas amirite
                var realColumn2 = flipH ? (7 - ((column * 2) + 1)) : ((column * 2) + 1);
                _spriteBuffer[(realRow * 8) + realColumn1] = (byte)(pixel1 + colorOffset);
                _spriteBuffer[(realRow * 8) + realColumn2] = (byte)(pixel2 + colorOffset);
            }
        }
    }

    private void WritePixel(int x, int y, byte colorIndex)
    {
        if (x < 0 || x >= DisplayWidth || y < 0 || y >= DisplayHeight)
            return;
        if (colorIndex == 0)
            return;

        var pixelIndex = (y * DisplayWidth) + x;

        _vRam.WriteByte(pixelIndex, colorIndex);
    }

    public void VBlank(OamBank oam)
    {
        _totalHudEntries = 0;
        FillBuffer(BackgroundColorIndex);

        if (Mode is not BlitterMode.NoOam and not BlitterMode.None)
        {
            ProcessOam(oam);
            ProcessHud();
        }
        if (Mode is not BlitterMode.NoText and not BlitterMode.None)
        {
            ProcessText();
        }
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
            {
                continue;
            }

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
                WritePixel(x + column, y + row, _spriteBuffer[(row * 8) + column]);
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
        _vRam.Fill(colorIndex);
    }
}
