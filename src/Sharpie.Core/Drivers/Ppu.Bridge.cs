namespace Sharpie.Core.Hardware;

public partial class Ppu
{
    private readonly byte[] _framebuffer = new byte[262144];

    public byte[] GetFrame()
    {
        var displayBase = DisplayStart;
        for (int i = 0; i <= ushort.MaxValue; i++)
        {
            var vramByteOffset = i / 2;
            var isHighNibble = (i & 1) == 0; // high nibble is always even pixel number
            var packed = _vRam.ReadByte(displayBase + vramByteOffset);
            var colorIndex = isHighNibble ? (byte)(packed >> 4) : (byte)(packed & 0xF);
            var realIndex = _systemRam.ReadByte(Memory.ColorPaletteStart + colorIndex);
            var color = IMotherboard.MasterPalette[realIndex];

            var bufferIndex = i * 4;
            _framebuffer[bufferIndex] = color.R;
            _framebuffer[bufferIndex + 1] = color.G;
            _framebuffer[bufferIndex + 2] = color.B;
            _framebuffer[bufferIndex + 3] = (byte)((realIndex == 0) ? 0 : 255);
        }
        return _framebuffer;
    }
}
