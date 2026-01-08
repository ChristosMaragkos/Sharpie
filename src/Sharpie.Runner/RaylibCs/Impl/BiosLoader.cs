using System.Reflection;

public static class BiosLoader
{
    public static byte[] GetEmbeddedBiosBinary()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("bios.bin");

        if (stream == null)
            throw new ApplicationException("Hardware Failure: Sharpie BIOS binary not found.");

        byte[] ba = new byte[stream.Length];
        stream.ReadExactly(ba, 0, ba.Length);
        return ba;
    }
}
