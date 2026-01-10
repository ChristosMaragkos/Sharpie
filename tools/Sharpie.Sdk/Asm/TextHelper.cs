public static class TextHelper
{
    public static byte GetFontIndex(char c)
    {
        if (c >= 'A' && c <= 'Z')
            return (byte)(c - 'A'); // A=0, B=1...
        if (c >= '0' && c <= '9')
            return (byte)(c - '0' + 26); // 0=26, 1=27...
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
            'v' => 45,
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
            _ => 56, // Default to space
        };
    }
}
