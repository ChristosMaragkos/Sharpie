namespace Sharpie.Assembler;

public class SharpieAssembler
{
    private readonly bool _isFirmware;

    public SharpieAssembler(bool isFirmware = false)
    {
        _isFirmware = isFirmware;
    }

    public byte[] AssembleFromFile(string inputFilePath)
    {
        var assembler = new SharpieRomEmitter(_isFirmware);
        var romData = assembler.LoadFile(inputFilePath);
        return romData;
    }

    public byte[] AssembleFromText(string sourceText)
    {
        var assembler = new SharpieRomEmitter(_isFirmware);
        return assembler.LoadRawAsm(sourceText);
    }
}
