using Raylib_cs;

namespace Sharpie.Runner.RaylibCs.Impl;

public class RaylibVideoOutput : IDisplayOutput
{
    private Texture2D _screenTexture;
    private int _resolution;

    public unsafe void HandleFramebuffer(byte[] frameBuffer)
    {
        fixed (byte* pFrame = frameBuffer)
        {
            Raylib.UpdateTexture(_screenTexture, pFrame);
        }

        var screenW = GetWindowWidth();
        var screenH = GetWindowHeight();
        float minDim = Math.Min(screenW, screenH);
        var xOffset = (screenW - minDim) / 2;
        var yOffset = (screenH - minDim) / 2;

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Raylib.DrawTexturePro(
            _screenTexture,
            new Rectangle(0, 0, _resolution, _resolution),
            new Rectangle(xOffset, yOffset, minDim, minDim),
            System.Numerics.Vector2.Zero,
            0f,
            Color.White
        );
        Raylib.EndDrawing();
    }

    public void Cleanup()
    {
        Raylib.UnloadTexture(_screenTexture);
        Raylib.CloseWindow();
    }

    public void Initialize(int internalRes, string title)
    {
        _resolution = internalRes;

        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(internalRes, internalRes, title);
        Raylib.SetTargetFPS(60);

        var blank = Raylib.GenImageColor(_resolution, _resolution, Color.Blank);
        _screenTexture = Raylib.LoadTextureFromImage(blank);
        Raylib.UnloadImage(blank);

        Raylib.SetTextureFilter(_screenTexture, TextureFilter.Point);
    }

    public bool ShouldCloseWindow() => Raylib_cs.Raylib.WindowShouldClose();

    public int GetWindowHeight() => Raylib.GetScreenHeight();

    public int GetWindowWidth() => Raylib.GetScreenWidth();
}
