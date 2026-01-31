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
        _motherboard.SaveRequested += OnSaveRequested;
    }

    public void Step() => _motherboard.Step();

    public void LoadBios(byte[] biosData) => _motherboard.LoadBios(biosData);

    public void LoadCartridge(byte[] fileData) => _motherboard.LoadCartridge(fileData);

    public byte[] GetVideoBuffer() => _motherboard.GetVideoBuffer();

    public static unsafe void FillAudioBufferRange(float* audioBuffer, uint sampleAmount) =>
        Motherboard.FillAudioBufferRange(audioBuffer, sampleAmount);

    public static void FillAudioBufferRange(float[] audioBuffer, int sampleAmount) =>
        Motherboard.FillAudioBufferRange(audioBuffer, sampleAmount);

    public ReadOnlySpan<byte> GetSaveRam() => _motherboard.SaveRam();

    public void LoadSaveData(byte[] saveData) => _motherboard.LoadSaveData(saveData);

    private void OnSaveRequested()
    {
        Console.WriteLine($"A save was requested at {DateTime.Now}");
        Save?.Invoke(GetSaveRam());
    }

    public event Action<ReadOnlySpan<byte>>? Save;
}
