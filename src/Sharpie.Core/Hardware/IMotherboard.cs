namespace Sharpie.Core.Hardware;

internal interface IMotherboard
{
    public static readonly Version BiosVersion = new(0, 2);
    public static readonly string VersionString =
        $"{BiosVersion.Major}.{BiosVersion.Minor}.{BiosVersion.Build}";
    public static ushort VersionBinFormat =>
        (ushort)(((BiosVersion.Major & 0xFF) << 8) | (BiosVersion.Minor & 0xFF));
    byte[] ControllerStates { get; }
    byte[,] TextGrid { get; }
    byte FontColorIndex { get; }

    void Step();
    void ClearScreen(byte colorIndex);
    void VBlank();
    void SetTextAttributes(byte attributes);
    void DrawChar(int x, int y, byte charCode);

    void InvokeSave();

    void PlayNote(
        byte channel,
        byte note,
        byte instrument,
        bool priority = false,
        bool allowOverride = false
    );
    void StopChannel(byte channel);
    void StopAllSounds();
    void StartSequencer(ushort address);

    void GetInputState();

    void SwapColor(byte oldIndex, byte newIndex);
    void SetCurrentBank(byte bankIndex);
    void StopSystem();
    ushort CheckCollision(int srcIndex);

    byte ReadByte(ushort address);
    byte ReadByte(int address);

    void WriteByte(ushort address, byte value);
    void WriteByte(int address, byte value);

    ushort ReadWord(ushort address);
    ushort ReadWord(int address);

    void WriteWord(ushort address, ushort value);
    void WriteWord(int address, ushort value);

    void FillRange(int startIndex, int amount, byte value);

    void PushDebug(string message);
    void ToggleSequencer();

    int GetOamCursor();
    void SetOamCursor(int value);
    void WriteSpriteEntry(ushort x, ushort y, byte tileId, byte attr, byte type);
    (ushort X, ushort Y, byte TileId, byte Attr, byte Type) ReadSpriteEntry(int index);

    ushort GetSequencerCursor();
    void SetSequencerCursor(ushort value);

    void MoveCamera(int dx, int dy);
    void SetCamera(ushort x, ushort y);

    void TriggerSegfault(SegfaultType segfaultType);

    void DefineInstrument(int index, byte a, byte d, byte s, byte r);
    public (byte Attack, byte Decay, byte Sustain, byte Release) ReadInstrument(int index);

    public static ReadOnlySpan<byte> SmallFont =>
        new byte[]
        {
            0x18,
            0x24,
            0x42,
            0x42,
            0x7E,
            0x42,
            0x42,
            0x42, // Char Index 0 - A
            0x38,
            0x44,
            0x44,
            0x44,
            0x5C,
            0x42,
            0x42,
            0x3C, // Char Index 1 - B
            0x3C,
            0x42,
            0x40,
            0x40,
            0x40,
            0x40,
            0x42,
            0x3C, // Char Index 2 - C
            0x78,
            0x44,
            0x42,
            0x42,
            0x42,
            0x42,
            0x44,
            0x78, // Char Index 3 - D
            0x3C,
            0x42,
            0x40,
            0x40,
            0x78,
            0x40,
            0x42,
            0x3C, // Char Index 4 - E
            0x3E,
            0x40,
            0x40,
            0x40,
            0x78,
            0x40,
            0x40,
            0x40, // Char Index 5 - F
            0x3C,
            0x42,
            0x40,
            0x40,
            0x4C,
            0x42,
            0x42,
            0x3C, // Char Index 6 - G
            0x42,
            0x42,
            0x42,
            0x42,
            0x7E,
            0x42,
            0x42,
            0x42, // Char Index 7 - H
            0x7E,
            0x08,
            0x08,
            0x08,
            0x10,
            0x10,
            0x10,
            0x7E, // Char Index 8 - I
            0x7E,
            0x04,
            0x04,
            0x04,
            0x04,
            0x24,
            0x24,
            0x18, // Char Index 9 - J
            0x42,
            0x42,
            0x42,
            0x44,
            0x78,
            0x44,
            0x42,
            0x42, // Char Index 10 - K
            0x40,
            0x40,
            0x40,
            0x40,
            0x40,
            0x40,
            0x40,
            0x3E, // Char Index 11 - L
            0x42,
            0x66,
            0x5A,
            0x42,
            0x42,
            0x42,
            0x42,
            0x42, // Char Index 12 - M
            0x44,
            0x22,
            0x22,
            0x32,
            0x2A,
            0x2A,
            0x24,
            0x44, // Char Index 13 - N
            0x3C,
            0x42,
            0x42,
            0x42,
            0x42,
            0x42,
            0x42,
            0x3C, // Char Index 14 - O
            0x3C,
            0x42,
            0x42,
            0x42,
            0x7C,
            0x40,
            0x40,
            0x40, // Char Index 15 - P
            0x3C,
            0x42,
            0x42,
            0x42,
            0x52,
            0x4A,
            0x44,
            0x3A, // Char Index 16 - Q
            0x7C,
            0x42,
            0x42,
            0x42,
            0x7C,
            0x42,
            0x42,
            0x42, // Char Index 17 - R
            0x18,
            0x26,
            0x40,
            0x40,
            0x3C,
            0x02,
            0x02,
            0x3C, // Char Index 18 - S
            0x7E,
            0x08,
            0x08,
            0x08,
            0x10,
            0x10,
            0x10,
            0x10, // Char Index 19 - T
            0x42,
            0x42,
            0x42,
            0x42,
            0x42,
            0x42,
            0x42,
            0x3C, // Char Index 20 - U
            0x42,
            0x42,
            0x42,
            0x42,
            0x42,
            0x24,
            0x24,
            0x18, // Char Index 21 - V
            0x42,
            0x42,
            0x42,
            0x42,
            0x5A,
            0x66,
            0x42,
            0x42, // Char Index 22 - W
            0x42,
            0x42,
            0x42,
            0x42,
            0x3C,
            0x42,
            0x42,
            0x42, // Char Index 23 - X
            0x42,
            0x42,
            0x24,
            0x24,
            0x18,
            0x08,
            0x08,
            0x18, // Char Index 24 - Y
            0x7E,
            0x02,
            0x04,
            0x08,
            0x10,
            0x20,
            0x40,
            0x7E, // Char Index 25 - Z
            0x18,
            0x24,
            0x52,
            0x52,
            0x4A,
            0x4A,
            0x24,
            0x18, // Char Index 26 - 0
            0x08,
            0x18,
            0x28,
            0x48,
            0x08,
            0x08,
            0x08,
            0x7E, // Char Index 27 - 1
            0x3C,
            0x42,
            0x02,
            0x02,
            0x3C,
            0x40,
            0x40,
            0x3E, // Char Index 28 - 2
            0x3C,
            0x42,
            0x02,
            0x02,
            0x1E,
            0x02,
            0x42,
            0x3C, // Char Index 29 - 3
            0x42,
            0x42,
            0x42,
            0x42,
            0x3E,
            0x02,
            0x02,
            0x02, // Char Index 30 - 4
            0x3E,
            0x40,
            0x40,
            0x40,
            0x3C,
            0x02,
            0x02,
            0x7C, // Char Index 31 - 5
            0x1E,
            0x20,
            0x40,
            0x40,
            0x7C,
            0x42,
            0x42,
            0x3C, // Char Index 32 - 6
            0x7E,
            0x02,
            0x02,
            0x04,
            0x3E,
            0x04,
            0x04,
            0x04, // Char Index 33 - 7
            0x3C,
            0x42,
            0x42,
            0x3C,
            0x42,
            0x42,
            0x42,
            0x3C, // Char Index 34 - 8
            0x3C,
            0x42,
            0x42,
            0x42,
            0x3E,
            0x02,
            0x42,
            0x3C, // Char Index 35 - 9
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x18,
            0x18, // Char Index 36 - .
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x18,
            0x08,
            0x10, // Char Index 37 - ,
            0x18,
            0x18,
            0x18,
            0x18,
            0x18,
            0x18,
            0x00,
            0x18, // Char Index 38 - !
            0x38,
            0x44,
            0x44,
            0x04,
            0x08,
            0x08,
            0x00,
            0x08, // Char Index 39 - ?
            0x7E,
            0x40,
            0x40,
            0x40,
            0x40,
            0x40,
            0x40,
            0x7E, // Char Index 40 - [
            0x7E,
            0x02,
            0x02,
            0x02,
            0x02,
            0x02,
            0x02,
            0x7E, // Char Index 41 - ]
            0x1E,
            0x20,
            0x20,
            0x40,
            0x40,
            0x20,
            0x20,
            0x1E, // Char Index 42 - (
            0x78,
            0x04,
            0x04,
            0x02,
            0x02,
            0x04,
            0x04,
            0x78, // Char Index 43 - )
            0x18,
            0x3C,
            0x7E,
            0xFF,
            0x18,
            0x18,
            0x18,
            0x18, // Char Index 44 - UpArrow
            0x18,
            0x18,
            0x18,
            0x18,
            0xFF,
            0x7E,
            0x3C,
            0x18, // Char Index 45 - DownArrow
            0x10,
            0x30,
            0x70,
            0xFF,
            0xFF,
            0x70,
            0x30,
            0x10, // Char Index 46 - LeftArrow
            0x08,
            0x0C,
            0x0E,
            0xFF,
            0xFF,
            0x0E,
            0x0C,
            0x08, // Char Index 47 - RightArrow
            0x02,
            0x02,
            0x04,
            0x08,
            0x10,
            0x20,
            0x40,
            0x40, // Char Index 48 - /
            0x00,
            0x00,
            0x00,
            0x00,
            0x7E,
            0x00,
            0x00,
            0x00, // Char Index 49 - -
            0x00,
            0x18,
            0x18,
            0x7E,
            0x7E,
            0x18,
            0x18,
            0x00, // Char Index 50 - +
            0x00,
            0x00,
            0xFF,
            0x00,
            0x00,
            0xFF,
            0x00,
            0x00, // Char Index 51 - =
            0x02,
            0x22,
            0x24,
            0x08,
            0x10,
            0x24,
            0x44,
            0x40, // Char Index 52 - %
            0x44,
            0x44,
            0x44,
            0x22,
            0x00,
            0x00,
            0x00,
            0x00, // Char Index 53 - "
            0x00,
            0x18,
            0x18,
            0x00,
            0x18,
            0x08,
            0x10,
            0x00, // Char Index 54 - ;
            0x00,
            0x18,
            0x18,
            0x00,
            0x00,
            0x18,
            0x18,
            0x00, // Char Index 55 - :
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00,
            0x00, // Char Index 56 - SPACE
        };

    internal static byte[] GetCharacter(int index)
    {
        var pixels = new byte[8];
        for (var i = 0; i < 8; i++)
        {
            pixels[i] = SmallFont[8 * index + i];
        }
        return pixels;
    }

    internal static ReadOnlySpan<(byte R, byte G, byte B)> MasterPalette =>
        new (byte R, byte G, byte B)[]
        {
            (0, 0, 0), // 0: Transparent/Background
            (255, 255, 255), // 1: White (Default Text)
            (245, 25, 25), // 2: Red
            (50, 31, 246), // 3: Blue
            (16, 239, 39), // 4: Green
            (247, 255, 15), // 5: Yellow
            (230, 28, 215), // 6: Pink/Magenta
            (102, 41, 166), // 7: Purple
            (14, 146, 26), // 8: Dark Green
            (243, 109, 0), // 9: Orange
            (77, 40, 0), // 10: Brown
            (186, 153, 14), // 11: Gold
            (162, 47, 47), // 12: Maroon
            (56, 90, 250), // 13: Light Blue
            (79, 79, 79), // 14: Dark Grey
            (0, 0, 0), // 15: True Black
            (255, 144, 144), // 16: Salmon/Peach
            (233, 148, 101), // 17: Tan
            (253, 73, 73), // 18: Bright Red
            (7, 217, 168), // 19: Teal
            (160, 242, 73), // 20: Lime
            (255, 213, 36), // 21: Amber
            (255, 123, 211), // 22: Hot Pink
            (179, 107, 207), // 23: Lavender
            (131, 239, 16), // 24: Electric Green
            (255, 152, 59), // 25: Light Orange
            (114, 65, 12), // 26: Deep Brown
            (203, 255, 92), // 27: Neon Yellow
            (94, 255, 164), // 28: Mint
            (244, 86, 190), // 29: Rose
            (56, 237, 255), // 30: Sky Blue
            (27, 27, 27), // 31: Rich Black
        };
}
