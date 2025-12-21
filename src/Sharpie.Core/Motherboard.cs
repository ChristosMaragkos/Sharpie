using Raylib_cs;

namespace Sharpie.Core;

public class Motherboard : IMotherboard
{
    private readonly Cpu _cpu;
    private readonly Ppu _ppu;
    private readonly Memory _memory;
    private Texture2D _screenTexture;
    private Image _screenImage;
    private byte _fontColorReg = 1;
    private byte _fontSizeReg = 0;

    public Motherboard()
    {
        _memory = new Memory();
        Span<byte> oam = _memory.Slice(Memory.OamStart, 2048);
        oam.Fill(0xFF);
        _cpu = new Cpu(_memory, this);
        _ppu = new Ppu(_memory);
    }

    public void LoadData(byte[] rom) => _memory.LoadData(0, rom);

    public void SetupDisplay()
    {
        Raylib.InitWindow(512, 512, "Sharpie");
        Raylib.SetTargetFPS(60);
        _screenImage = new Image
        {
            Width = 256,
            Height = 256,
            Mipmaps = 1,
            Format = PixelFormat.UncompressedR8G8B8A8,
        };
        _screenTexture = Raylib.LoadTextureFromImage(_screenImage);
        Raylib.SetTextureFilter(_screenTexture, TextureFilter.Point);
    }

    public void UpdateDisplay()
    {
        byte[] displaydata = _ppu.GetFrame();
        unsafe
        {
            fixed (byte* pDisp = displaydata)
            {
                Raylib.UpdateTexture(_screenTexture, pDisp);
            }
        }
    }

    public void AwaitVBlank()
    {
        _ppu.VBlank();
    }

    public void ClearScreen(byte colorIndex)
    {
        _ppu.FillBuffer(colorIndex);
    }

    public void DrawChar(ushort x, ushort y, ushort charCode)
    {
        _ppu.BlitCharacter(x, y, _fontColorReg, IMotherboard.GetCharacter(charCode));
    }

    public ushort GetInputState(byte controllerIndex)
    {
        ushort state = 0; // TODO: Switch case for two controller schemes

        if (Raylib.IsKeyDown(KeyboardKey.Up))
            state |= 1;
        if (Raylib.IsKeyDown(KeyboardKey.Down))
            state |= 2;
        if (Raylib.IsKeyDown(KeyboardKey.Left))
            state |= 4;
        if (Raylib.IsKeyDown(KeyboardKey.Right))
            state |= 8;
        if (Raylib.IsKeyDown(KeyboardKey.Z))
            state |= 16;
        if (Raylib.IsKeyDown(KeyboardKey.X))
            state |= 32;
        if (Raylib.IsKeyDown(KeyboardKey.LeftShift))
            state |= 64;
        if (Raylib.IsKeyDown(KeyboardKey.Tab))
            state |= 128;

        return state;
    }

    public void PlayNote(byte channel, byte note)
    {
        throw new NotImplementedException();
    }

    public void SetTextAttributes(byte attributes)
    {
        _fontColorReg = (byte)(attributes & 0x0F);
        _fontSizeReg = (byte)((attributes >> 4) & 0x0F);
    }

    public void StopChannel(byte channel)
    {
        throw new NotImplementedException();
    }

    public void StopSystem()
    {
        throw new NotImplementedException();
    }

    public void SwapColor(byte oldIndex, byte newIndex)
    {
        _memory.WriteByte(Memory.ColorPaletteStart + oldIndex, newIndex);
    }

    public void Run()
    {
        SetupDisplay();
        byte x = 10;
        byte y = 10;

        while (!Raylib.WindowShouldClose())
        {
            _ppu.FillBuffer(0);
            for (var i = 0; i < 16000; i++)
                _cpu.Cycle();
            ushort input = GetInputState(0);
            if ((input & 1) != 0)
                y--;
            if ((input & 2) != 0)
                y++;
            if ((input & 4) != 0)
                x--;
            if ((input & 8) != 0)
                x++;

            DrawChar(0, 0, 2);
            AwaitVBlank();
            _ppu.FlipBuffers();
            UpdateDisplay();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            Raylib.DrawTexturePro(
                _screenTexture,
                new Rectangle(0, 0, 256, 256),
                new Rectangle(0, 0, 512, 512),
                new System.Numerics.Vector2(0, 0),
                0f,
                Color.White
            );
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
