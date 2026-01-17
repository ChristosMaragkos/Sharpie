namespace Sharpie.Sdk.Gui;

public static class Helpers
{
    public static byte[]? GetEmbeddedFontFile()
    {
        using var stream = typeof(Helpers).Assembly.GetManifestResourceStream("Tahoma.ttf");
        if (stream == null)
            return null;

        var fontData = new byte[stream.Length];
        stream.ReadExactly(fontData, 0, fontData.Length);

        return fontData;
    }
}
