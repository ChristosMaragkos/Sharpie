namespace Sharpie.Sdk.Meta;

internal static class Constants
{
    public static readonly Version BiosVersion = new Version(0, 1);
    public static string VersionString =>
        $"{BiosVersion.Major}.{BiosVersion.Minor}.{BiosVersion.Build}";
    public static ushort VersionBinFormat =>
        (ushort)(((BiosVersion.Major & 0xFF) << 8) | (BiosVersion.Minor & 0xFF));
    public const string MagicHeader = "SHRP";
    public const int HeaderSize = 64;
    public const int TitleLimit = 24;
    public const int AuthorLimit = 16;
    public const int MaxRomSize = 59392;

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
