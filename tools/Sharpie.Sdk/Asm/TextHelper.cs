public static class TextHelper
{
    // Glyph index layout (1-byte internal representation, unchanged):
    //   0-25  : A-Z  (uppercase letters)
    //   26-35 : 0-9  (digits)
    //   36    : .
    //   37    : ,
    //   38    : !
    //   39    : ?    ← unknown-character fallback
    //   40    : [
    //   41    : ]
    //   42    : (
    //   43    : )
    //   44    : ^ (up arrow)
    //   45    : v (down arrow)
    //   46    : < (left arrow)
    //   47    : > (right arrow)
    //   48    : /
    //   49    : -
    //   50    : +
    //   51    : =
    //   52    : %
    //   53    : " / '
    //   54    : ;
    //   55    : :
    //   56    : SPACE

    /// <summary>
    /// Encodes a character as a single-byte ASCII value.
    /// Non-ASCII input falls back to '?'.
    /// </summary>
    public static byte AsciiToByte(char c) => c <= 0x7F ? (byte)c : (byte)'?';

    /// <summary>
    /// Decodes a byte to ASCII, replacing non-ASCII bytes with '?'.
    /// </summary>
    public static char ByteToAscii(byte value) => value <= 0x7F ? (char)value : '?';

    /// <summary>
    /// Maps an ASCII (or Unicode) character to a 1-byte Sharpie glyph index.
    /// Lowercase letters are folded to their uppercase equivalents.
    /// Unrecognized characters produce the '?' glyph (index 39).
    /// </summary>
    public static byte AsciiToGlyphIndex(char c)
    {
        // Fold lowercase → uppercase so both cases share the same glyph.
        if (c >= 'a' && c <= 'z')
            c = (char)(c - 32);

        if (c >= 'A' && c <= 'Z')
            return (byte)(c - 'A'); // A=0 … Z=25
        if (c >= '0' && c <= '9')
            return (byte)(c - '0' + 26); // 0=26 … 9=35

        return c switch
        {
            '.' => 36,
            ',' => 37,
            '!' => 38,
            '?' => 39,
            '[' => 40,
            ']' => 41,
            '(' => 42,
            ')' => 43,
            '^' => 44,
            'V' => 45, // down-arrow glyph (lowercase already folded above)
            '<' => 46,
            '>' => 47,
            '/' => 48,
            '-' => 49,
            '+' => 50,
            '=' => 51,
            '%' => 52,
            '"' or '\'' => 53,
            ';' => 54,
            ':' => 55,
            ' ' => 56,
            _ => 39, // unknown → '?' glyph (not space)
        };
    }

    /// <summary>
    /// Backwards-compat alias — delegates to <see cref="AsciiToGlyphIndex"/>.
    /// </summary>
    public static byte GetFontIndex(char c) => AsciiToGlyphIndex(c);

    /// <summary>
    /// Maps a Sharpie glyph index back to its representative ASCII character.
    /// Used by the core for debug output / serialization.
    /// </summary>
    public static char GlyphIndexToAscii(byte index) =>
        index switch
        {
            <= 25 => (char)('A' + index),
            >= 26 and <= 35 => (char)('0' + (index - 26)),
            36 => '.',
            37 => ',',
            38 => '!',
            39 => '?',
            40 => '[',
            41 => ']',
            42 => '(',
            43 => ')',
            44 => '^',
            45 => 'v',
            46 => '<',
            47 => '>',
            48 => '/',
            49 => '-',
            50 => '+',
            51 => '=',
            52 => '%',
            53 => '"',
            54 => ';',
            55 => ':',
            56 => ' ',
            _ => '?',
        };
}
