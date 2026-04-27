using StbImageSharp;

namespace Sharpie.Tools;

public enum OutputFormat
{
    Assembly,
    C,
}

public class ImageConverter
{
    private static readonly (byte R, byte G, byte B)[] MasterPalette =
    [
        (0, 0, 0),
        (255, 255, 255),
        (245, 25, 25),
        (50, 31, 246),
        (16, 239, 39),
        (247, 255, 15),
        (230, 28, 215),
        (102, 41, 166),
        (14, 146, 26),
        (243, 109, 0),
        (77, 40, 0),
        (186, 153, 14),
        (162, 47, 47),
        (56, 90, 250),
        (79, 79, 79),
        (0, 0, 0),
        // alt palette
        (255, 144, 144),
        (233, 148, 101),
        (253, 73, 73),
        (7, 217, 168),
        (160, 242, 73),
        (255, 213, 36),
        (255, 123, 211),
        (179, 107, 207),
        (131, 239, 16),
        (255, 152, 59),
        (114, 65, 12),
        (203, 255, 92),
        (94, 255, 164),
        (244, 86, 190),
        (56, 237, 255),
        (27, 27, 27),
    ];

    public static void ConvertImage(
        string inputPath,
        string outputDirectory,
        OutputFormat format,
        bool failOnPaletteMix = true
    )
    {
        string baseName = Path.GetFileNameWithoutExtension(inputPath)
            .Replace("-", "_")
            .Replace(" ", "_")
            .ToLower();

        using var stream = File.OpenRead(inputPath);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        if (image.Width % 8 != 0 || image.Height % 8 != 0)
            throw new Exception(
                $"Image dimensions ({image.Width}x{image.Height}) must be a multiple of 8x8."
            );

        if (format == OutputFormat.Assembly)
            GenerateAssembly(image, baseName, outputDirectory, failOnPaletteMix);
        else
            GenerateC(image, baseName, outputDirectory, failOnPaletteMix);
    }

    private static void GenerateAssembly(
        ImageResult image,
        string baseName,
        string outputDirectory,
        bool strictMode
    )
    {
        string asmPath = Path.Combine(outputDirectory, $"{baseName}.asm");
        using var writer = new StreamWriter(asmPath);

        writer.WriteLine($"; Auto-generated sprite: {baseName}");
        writer.WriteLine();

        int spriteIndex = 0;
        for (int startY = 0; startY < image.Height; startY += 8)
        {
            for (int startX = 0; startX < image.Width; startX += 8)
            {
                writer.Write($".SPRITE {spriteIndex} ");
                ProcessSpriteBlock(
                    writer,
                    image,
                    startX,
                    startY,
                    spriteIndex++,
                    strictMode,
                    OutputFormat.Assembly
                );
                writer.WriteLine();
            }
        }
        Console.WriteLine($"Successfully generated {baseName}.asm ({spriteIndex} sprites).");
    }

    private static void GenerateC(
        ImageResult image,
        string baseName,
        string outputDirectory,
        bool strictMode
    )
    {
        string headerPath = Path.Combine(outputDirectory, $"{baseName}.h");
        string sourcePath = Path.Combine(outputDirectory, $"{baseName}.c");

        int spriteCount = image.Width / 8 * (image.Height / 8);

        using (var hWriter = new StreamWriter(headerPath))
        {
            string guard = $"{baseName.ToUpper()}_H";
            hWriter.WriteLine($"#ifndef {guard}\n#define {guard}\n");
            hWriter.WriteLine($"#define {baseName.ToUpper()}_START_ID 0");
            hWriter.WriteLine($"#define {baseName.ToUpper()}_SPRITE_COUNT {spriteCount}\n");
            hWriter.WriteLine($"#endif // {guard}");
        }

        int spriteIndex = 0;
        using (var cWriter = new StreamWriter(sourcePath))
        {
            cWriter.WriteLine($"#include \"{baseName}.h\"\n");
            cWriter.WriteLine("asm(");
            cWriter.WriteLine("    \".REGION SPRITE_ATLAS\\n\"");

            for (int startY = 0; startY < image.Height; startY += 8)
            {
                for (int startX = 0; startX < image.Width; startX += 8)
                {
                    cWriter.WriteLine($"    \".SPRITE {spriteIndex}\\n\"");
                    cWriter.Write("    \"    .DB ");

                    ProcessSpriteBlock(
                        cWriter,
                        image,
                        startX,
                        startY,
                        spriteIndex++,
                        strictMode,
                        OutputFormat.C
                    );

                    cWriter.WriteLine("\\n\"");
                }
            }

            cWriter.WriteLine("    \".ENDREGION\\n\"");
            cWriter.WriteLine(");");
        }

        Console.WriteLine(
            $"Successfully generated {baseName}.h and {baseName}.c ({spriteCount} sprites)."
        );
    }

    private static void ProcessSpriteBlock(
        StreamWriter writer,
        ImageResult image,
        int startX,
        int startY,
        int spriteIndex,
        bool strict,
        OutputFormat format
    )
    {
        bool usesNormal = false;
        bool usesAlt = false;

        for (int y = 0; y < 8; y++)
        {
            if (format == OutputFormat.Assembly)
                writer.Write("   .DB ");

            for (int x = 0; x < 8; x += 2)
            {
                int p1Index = MatchColor(GetPixel(image, startX + x, startY + y));
                int p2Index = MatchColor(GetPixel(image, startX + x + 1, startY + y));

                if (p1Index > 0 && p1Index < 16)
                    usesNormal = true;
                if (p1Index > 16)
                    usesAlt = true;
                if (p2Index > 0 && p2Index < 16)
                    usesNormal = true;
                if (p2Index > 16)
                    usesAlt = true;

                byte packed = (byte)(((p1Index % 16) << 4) | (p2Index % 16));

                writer.Write($"0x{packed:X2}");

                if (format == OutputFormat.Assembly)
                {
                    if (x < 6)
                        writer.Write(", ");
                }
                else if (format == OutputFormat.C)
                {
                    if (y < 7 || x < 6)
                        writer.Write(", ");
                }
            }

            if (format == OutputFormat.Assembly)
                writer.WriteLine();
        }

        if (usesNormal && usesAlt)
        {
            string msg = $"Sprite {spriteIndex} uses colors from both the normal and alt palettes.";
            if (strict)
                throw new Exception("BUILD FAILED: " + msg);
            else
                Console.WriteLine("WARNING: " + msg);
        }
    }

    private static (byte R, byte G, byte B, byte A) GetPixel(ImageResult img, int x, int y)
    {
        int index = (y * img.Width + x) * 4;
        return (img.Data[index], img.Data[index + 1], img.Data[index + 2], img.Data[index + 3]);
    }

    private static int MatchColor((byte R, byte G, byte B, byte A) pixel)
    {
        if (pixel.A < 128)
            return 0;

        int bestIndex = 0;
        int shortestDistance = int.MaxValue;

        for (int i = 0; i < MasterPalette.Length; i++)
        {
            var (R, G, B) = MasterPalette[i];
            int dR = pixel.R - R;
            int dG = pixel.G - G;
            int dB = pixel.B - B;
            int distance = (dR * dR) + (dG * dG) + (dB * dB);

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                bestIndex = i;
            }
        }
        return bestIndex;
    }
}
