using Sharpie.Core;
using Sharpie.Core.Drivers;

namespace Sharpie.Runner.Headless.Impl;

internal class HeadlessAudioOutput : IAudioOutput
{
    public void Cleanup() { }

    public void HandleAudioBuffer(float[] audioBuffer) { }

    public void Initialize(int sampleRate) { }
}

internal class HeadlessInputHandler : InputHandler
{
    public override (byte, byte) GetInputState()
    {
        return (0, 0);
    }
}

internal class HeadlessVideoOutput : IDisplayOutput
{
    public void Cleanup() { }

    public int GetWindowHeight()
    {
        return 0;
    }

    public int GetWindowWidth()
    {
        return 0;
    }

    public void HandleFramebuffer(byte[] frameBuffer) { }

    public void Initialize(int internalResolution, string windowTitle) { }

    public bool ShouldCloseWindow(SharpieConsole? emulator = null)
    {
        return emulator?.IsHalted ?? false;
    }
}

internal class HeadlessSaveHandler : ISaveHandler
{
    public string? SavePath => string.Empty;

    public void SaveToDisk(ReadOnlySpan<byte> saveRam, bool append = false) { }
}

internal class HeadlessDebugOutput : DebugOutput
{
    public HeadlessDebugOutput(int size) : base(size) { }

    public override void Log(string message)
    {
        Console.WriteLine(message);
    }
}
