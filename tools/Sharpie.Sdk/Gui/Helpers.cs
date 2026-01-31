using Sharpie.Sdk.Meta;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
#if Windows
using System.Reflection;
#endif

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

#if Windows
    public unsafe static byte[] GetEmbeddedIcon()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("icon.png");

        if (stream == null)
            Console.WriteLine("Could not find an embedded .png file.");

        byte[] ba = new byte[stream.Length];
        stream.ReadExactly(ba, 0, ba.Length);
        return ba;
    }
#endif

    public static void BatchPngToAssembly(
        IEnumerable<string> inputFilePaths,
        ProjectManifest manifest,
        string outputFilePath
    )
    {
        if (string.IsNullOrWhiteSpace(outputFilePath))
            throw new InvalidOperationException("Invalid output path.");

        if (!outputFilePath.EndsWith(".asm"))
            throw new FormatException("Output must be an Assembly file (.asm).");

        var startIndex = 0;
        using var writer = new StreamWriter(outputFilePath, false);
        foreach (var path in inputFilePaths)
        {
            PngToAssembly(path, manifest, outputFilePath, writer, ref startIndex);
        }
    }

    private static void PngToAssembly(
        string inputFilePath,
        ProjectManifest manifest,
        string outputFilePath,
        TextWriter writer,
        ref int startIndex
    )
    {
        if (!inputFilePath.EndsWith(".png"))
            throw new FormatException("Input file must be a PNG image.");

        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"No file by name {inputFilePath} found.");

        const int SpriteSize = 8;
        const int MaxSprites = 256;

        using var image = Image.Load<Rgba32>(inputFilePath);

        var meta = image.Metadata.GetPngMetadata();
        if (meta.ColorType != PngColorType.Palette)
            throw new InvalidOperationException("PNG must be indexed (palette-based).");

        var clrTable = meta.ColorTable;
        if (!clrTable.HasValue)
            throw new InvalidOperationException("Indexed PNG has no color table.");

        var palette = clrTable.Value.Span;

        if (palette.Length == 0)
            throw new InvalidOperationException("Indexed PNG has no color palette.");

        if (palette.Length > 16)
            throw new InvalidOperationException("PNG palette may not exceed 16 colors.");

        var width = image.Width;
        var height = image.Height;

        if (width % SpriteSize != 0 || height % SpriteSize != 0)
            throw new InvalidOperationException("Image dimensions must be multiples of 8.");

        var spritesX = width / SpriteSize;
        var spritesY = height / SpriteSize;
        var spriteAmount = spritesX * spritesY;

        if (spriteAmount + startIndex > MaxSprites)
            throw new InvalidOperationException(
                $"Image {inputFilePath} would exceed the 256 sprite limit."
            );

        for (var spriteY = 0; spriteY < spritesY; spriteY++)
        {
            for (var spriteX = 0; spriteX < spritesX; spriteX++)
            {
                var sprIdx = (spriteY * spritesX + spriteX) + startIndex;
                writer.WriteLine($".SPRITE {sprIdx}");

                for (var row = 0; row < SpriteSize; row++)
                {
                    var rowBytes = new List<string>();

                    for (var col = 0; col < SpriteSize; col += 2)
                    {
                        var px = spriteX * SpriteSize + col;
                        var py = spriteY * SpriteSize + row;

                        var pixel1 = image[px, py];
                        var pixel2 = image[px + 1, py];
                        var idx1 = MapColorToPalette(pixel1, manifest);
                        var idx2 = MapColorToPalette(pixel2, manifest); // is it obvious I'm running out of variable names?

                        var packed = (byte)((idx1 << 4) | (idx2 & 0x0F));
                        rowBytes.Add($"0x{packed:X2}");
                    }

                    writer.WriteLine("\t.DB " + string.Join(", ", rowBytes));
                }

                writer.WriteLine();
            }
        }
        startIndex += spriteAmount;

        Console.WriteLine($"Exported {spriteAmount} sprites to {outputFilePath} as Assembly code!");
    }

    private static int MapColorToPalette(Rgba32 pixel, ProjectManifest manifest)
    {
        if (pixel.A < 128)
            return 0;

        var bestIndex = 0;
        var bestDist = double.MaxValue;

        for (var i = 0; i < manifest.Palette.Length; i++)
        {
            var masterIdx = manifest.Palette[i];
            if (masterIdx == 0xFF)
                masterIdx = i; // Same logic as the bootloader

            var (r, g, b) = Constants.MasterPalette[masterIdx];
            var dist =
                Math.Pow(pixel.R - r, 2) + Math.Pow(pixel.G - g, 2) + Math.Pow(pixel.B - b, 2);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
