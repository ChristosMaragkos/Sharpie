namespace Sharpie.Core.Hardware;

public interface IMotherboard
{
    public static readonly Version BiosVersion = new(0, 0, 1);
    public static readonly string VersionString =
        $"{BiosVersion.Major}.{BiosVersion.Minor}.{BiosVersion.Build}";
    public static ushort VersionBinFormat =>
        (ushort)(BiosVersion.Major * 10000 + BiosVersion.Minor * 100 + BiosVersion.Build);
    byte[] ControllerStates { get; }
    byte[,] TextGrid { get; }
    byte FontColorIndex { get; }

    void Step();
    void ClearScreen(byte colorIndex);
    void VBlank();
    void SetTextAttributes(byte attributes);
    void DrawChar(int x, int y, byte charCode);

    void PlayNote(byte channel, byte note, byte instrument);
    void StopChannel(byte channel);
    void StopAllSounds();
    void StartSequencer(ushort address);

    void GetInputState();

    void SwapColor(byte oldIndex, byte newIndex);
    void StopSystem();
    ushort CheckCollision(int sprIdSrc);

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
            (0, 0, 0), // 00: #000000 (Transparent Key)
            (255, 255, 255), // 01: #FFFFFF
            (89, 86, 82), // 02: #595652
            (95, 205, 228), // 03: #5FCDE4
            (91, 110, 225), // 04: #5B6EE1
            (153, 229, 80), // 05: #99E550
            (75, 105, 47), // 06: #4B692F
            (251, 242, 54), // 07: #FBF236
            (223, 113, 38), // 08: #DF7126
            (215, 123, 186), // 09: #D77BBA
            (172, 50, 50), // 10: #AC3232
            (238, 195, 154), // 11: #EEC39A
            (102, 57, 49), // 12: #663931
            (69, 40, 60), // 13: #45283C
            (34, 32, 52), // 14: #222034
            (13, 12, 23), // 15: #0D0C17
            (166, 169, 173), // 16: #A6A9AD
            (221, 223, 203), // 17: #DDDFCB
            (186, 215, 195), // 18: #BAD7C3
            (153, 198, 206), // 19: #99C6CE
            (17, 60, 101), // 20: #113C65
            (83, 205, 205), // 21: #53CDCD
            (40, 132, 69), // 22: #288445
            (32, 142, 217), // 23: #208ED9
            (4, 13, 201), // 24: #040DC9
            (180, 150, 208), // 25: #B496D0
            (102, 26, 175), // 26: #661AAF
            (164, 145, 30), // 27: #A4911E
            (82, 75, 36), // 28: #524B24
            (178, 64, 41), // 29: #B24029
            (77, 26, 12), // 30: #4D1A0C
            (0, 0, 0), // 31: #000000 (Solid Black)
        };
}
