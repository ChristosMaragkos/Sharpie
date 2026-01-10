using Sharpie.Core.Drivers;
using Sharpie.Core.Hardware;

public class SharpieConsole
{
    private readonly Motherboard _motherboard;
    public bool IsInBootMode => _motherboard.IsInBootMode;

    public SharpieConsole(
        IDisplayOutput display,
        IAudioOutput audio,
        InputHandler input,
        DebugOutput? debug
    )
    {
        _motherboard = new Motherboard(display, audio, input, debug);
    }

    public void Step() => _motherboard.Step();

    public void LoadBios(byte[] biosData) => _motherboard.LoadBios(biosData);

    public void LoadCartridge(byte[] fileData) => _motherboard.LoadCartridge(fileData);

    public byte[] GetVideoBuffer() => _motherboard.GetVideoBuffer();

    public static unsafe void FillAudioBufferRange(float* audioBuffer, uint sampleAmount) =>
        Motherboard.FillAudioBufferRange(audioBuffer, sampleAmount);
}
