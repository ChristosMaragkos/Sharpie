namespace Sharpie.Core.Drivers;

public interface IDisplayOutput
{
    void Initialize(int internalResolution, string windowTitle);
    bool ShouldCloseWindow(SharpieConsole? emulator = null);
    void Cleanup();
    void HandleFramebuffer(byte[] frameBuffer);

    int GetWindowHeight();
    int GetWindowWidth();
}
