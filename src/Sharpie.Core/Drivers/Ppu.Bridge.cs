namespace Sharpie.Core.Hardware;

internal partial class Ppu
{
    private readonly byte[] _framebuffer = new byte[262144];

    public byte[] GetFrame()
    {
        for (int i = 0; i < FrameSize; i++)
        {
            var realIndex = _vRam.ReadByte(i);
            var (R, G, B) = IMotherboard.MasterPalette[realIndex];

            var bufferIndex = i * 4;
            _framebuffer[bufferIndex] = R;
            _framebuffer[bufferIndex + 1] = G;
            _framebuffer[bufferIndex + 2] = B;
            _framebuffer[bufferIndex + 3] = (byte)(realIndex is 0 ? 0 : 255); // Color 16 (aka color 0 in the alternate palette) is also always ignored
        }
        return _framebuffer;
    }
}
