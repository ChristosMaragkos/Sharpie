namespace Sharpie.Sdk.Meta;

internal static class Constants
{
    private static readonly Version Version = new Version(0, 0, 1);
    public static string VersionString => $"{Version.Major}.{Version.Minor}.{Version.Build}";
    public static ushort VersionBinFormat =>
        (ushort)(Version.Major * 10000 + Version.Minor * 100 + Version.Build);
    public const string MagicHeader = "SHRP";
    public const int HeaderSize = 64;
    public const int TitleLimit = 24;
    public const int AuthorLimit = 16;
    public const int MaxRomSize = 59392;
}
