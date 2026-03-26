using System.Text;

namespace Sharpie.Assembler.Utilities;

public class SharpieExporter
{
    private const string MagicBytes = "SHRP";
    private const int TitleLength = 32;
    private const int AuthorLength = 32;
    private const int PaletteSize = 32;

    public string Title { get; set; } = "Untitled";
    public string Author { get; set; } = "Unknown";

    public byte[] CreateCartridge(byte[] compiledCode, bool isFirmware)
    {
        if (isFirmware)
        {
            return compiledCode;
        }

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Encoding.ASCII.GetBytes(MagicBytes));

        var titleBytes = new byte[TitleLength];
        Encoding.ASCII.GetBytes(Title.PadRight(TitleLength)).CopyTo(titleBytes, 0);
        writer.Write(titleBytes);

        var authorBytes = new byte[AuthorLength];
        Encoding.ASCII.GetBytes(Author.PadRight(AuthorLength)).CopyTo(authorBytes, 0);
        writer.Write(authorBytes);

        for (int i = 0; i < PaletteSize; i++)
        {
            writer.Write(0xFF); // Default palettes are being removed in v0.4, this is staying for legacy reasons
        }
        writer.Write(compiledCode);

        return ms.ToArray();
    }
}
