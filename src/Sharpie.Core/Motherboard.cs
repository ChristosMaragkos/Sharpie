using Raylib_cs;

namespace Sharpie.Core;

public class Motherboard : IMotherboard
{
    private readonly Cpu _cpu;
    private readonly Ppu _ppu;
    private readonly Apu _apu;
    private readonly Memory _memory;
    private readonly Sequencer _sequencer;

    private Texture2D _screenTexture;
    private RenderTexture2D _target;
    private AudioStream _stream;
    private float[] _writeBuffer = new float[441];
    private int _actualWindowSize;

    private byte _fontColorReg = 1;
    private byte _fontSizeReg = 0;

    private const int MusicPointerAddress = 0x0004;
    private int _musicStart = 0;

    public byte[] ControllerStates { get; } = new byte[2];

    public Motherboard()
    {
        _memory = new Memory();
        Span<byte> oam = _memory.Slice(Memory.OamStart, 2048);
        oam.Fill(0xFF);
        _cpu = new Cpu(_memory, this);
        _ppu = new Ppu(_memory);
        _apu = new Apu(_memory);
        _sequencer = new Sequencer(_memory);
    }

    public void LoadCartridge(string path)
    {
        var loadAttempt = Cartridge.Load(path);
        if (!loadAttempt.HasValue)
            return; // softlock like how the DS froze if you took the cartridge out

        var cart = loadAttempt.Value;
        _cpu.LoadPalette(cart.HeaderPalette);
        for (var i = 0; i < cart.RomData.Length; i++)
        {
            _memory.WriteByte(i, cart.RomData[i]);
        }

        _musicStart = cart.MusicAddress;
    }

    public void SetupDisplay()
    {
        const int internalRes = 256;

        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(100, 100, "Sharpie");

        var screenH = Raylib.GetMonitorHeight(0) - 200;

        var scale = (screenH / internalRes) - 2;
        if (scale < 1)
            scale = 1;

        _actualWindowSize = internalRes * scale;

        // 2. NOW set the real size and center it
        Raylib.SetWindowSize(_actualWindowSize, _actualWindowSize);
        Raylib.SetWindowPosition(
            (Raylib.GetMonitorWidth(0) - _actualWindowSize) / 2,
            (Raylib.GetMonitorHeight(0) - _actualWindowSize) / 2
        );

        Raylib.SetTargetFPS(60);

        // 3. Setup Textures
        _target = Raylib.LoadRenderTexture(internalRes, internalRes);
        var blank = Raylib.GenImageColor(internalRes, internalRes, Color.Blank);
        _screenTexture = Raylib.LoadTextureFromImage(blank);
        Raylib.UnloadImage(blank);

        Raylib.SetTextureFilter(_screenTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(_target.Texture, TextureFilter.Point);
    }

    private void UpdateDisplay()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black); // This draws the black bars
        RenderBufferAsTexture();

        int screenW = Raylib.GetScreenWidth();
        int screenH = Raylib.GetScreenHeight();

        // 1. Find the smallest dimension to keep it square
        float minDim = Math.Min(screenW, screenH);

        // 2. Center the square in the window
        float xOffset = (screenW - minDim) / 2;
        float yOffset = (screenH - minDim) / 2;

        var sourceRec = new Rectangle(0, 0, 256, -256);
        // 3. The destination is now a centered square
        var destRec = new Rectangle(xOffset, yOffset, minDim, minDim);

        Raylib.DrawTexturePro(
            _target.Texture,
            sourceRec,
            destRec,
            System.Numerics.Vector2.Zero,
            0f,
            Color.White
        );
        Raylib.EndDrawing();
    }

    public void SetupAudio()
    {
        Raylib.SetAudioStreamBufferSizeDefault(1764);
        Raylib.InitAudioDevice();
        // 44100Hz 32bit float mono
        _stream = Raylib.LoadAudioStream(44100, 32, 1);
        _writeBuffer = new float[1764]; // 10ms of audio at a time
        Raylib.PlayAudioStream(_stream);
        Raylib.SetMasterVolume(1f);
    }

    public unsafe void UpdateAudio()
    {
        if (!Raylib.IsAudioStreamProcessed(_stream))
            return;

        _apu.FillBuffer(_writeBuffer);
        fixed (float* pBuffer = _writeBuffer)
        {
            Raylib.UpdateAudioStream(_stream, pBuffer, 1764);
        }
    }

    public void AwaitVBlank()
    {
        GetInputState();
        _ppu.VBlank();
        _ppu.FlipBuffers();
    }

    public void ClearScreen(byte colorIndex)
    {
        _ppu.FillBuffer(colorIndex);
    }

    public void DrawChar(ushort x, ushort y, ushort charCode)
    {
        _ppu.BlitCharacter(x, y, _fontColorReg, IMotherboard.GetCharacter(charCode));
    }

    public void GetInputState()
    {
        byte state = 0;

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

        ControllerStates[0] = state;
        ControllerStates[1] = 0; // TODO: Add support for second controller?
    }

    public void PlayNote(byte channel, byte note, byte instrument)
    {
        var freq = channel < 6 ? 440f * MathF.Pow(2f, (note - 69f) / 12f) : note;
        _apu.ResetPhase(channel);
        var baseAddr = Memory.AudioRamStart + (channel * 4);
        _memory.WriteWord(baseAddr, (ushort)freq);
        _memory.WriteByte(baseAddr + 2, 0xFF);
        _memory.WriteByte(baseAddr + 3, (byte)((instrument << 1) | 1));
    }

    public void SetTextAttributes(byte attributes)
    {
        _fontColorReg = (byte)(attributes & 0x0F);
        _fontSizeReg = (byte)((attributes >> 4) & 0x0F);
    }

    public void StopChannel(byte channel)
    {
        var contolAddr = Memory.AudioRamStart + (channel * 4) + 3;
        var control = _memory.ReadByte(contolAddr);
        _memory.WriteByte(contolAddr, (byte)(control & ~0x01));
    }

    public void StopSystem()
    {
        _cpu.Halt();
    }

    public void SwapColor(byte oldIndex, byte newIndex)
    {
        _memory.WriteByte(Memory.ColorPaletteStart + oldIndex, newIndex);
    }

    public void StopAllSounds()
    {
        _apu.ClearPhases();
    }

    public void StartSequencer(ushort address)
    {
        _sequencer.LoadSong(address);
    }

    private unsafe void RenderBufferAsTexture()
    {
        AwaitVBlank();
        var frameData = _ppu.GetFrame();
        fixed (byte* pPixels = frameData)
        {
            Raylib.UpdateTexture(_screenTexture, pPixels);
        }
        Raylib.BeginTextureMode(_target);
        Raylib.DrawTexture(_screenTexture, 0, 0, Color.White);
        Raylib.EndTextureMode();
    }

    public void Run()
    {
        SetupDisplay();
        SetupAudio();
        _cpu.Reset();
        while (!Raylib.WindowShouldClose())
        {
            for (var i = 0; i < 16000; i++)
                _cpu.Cycle();

            UpdateAudio();
            _sequencer.Step(this);
            UpdateDisplay();
        }

        Cleanup();
    }

    private void Cleanup()
    {
        Raylib.UnloadTexture(_screenTexture);
        Raylib.UnloadRenderTexture(_target);
        Raylib.CloseAudioDevice();
        Raylib.CloseWindow();
    }
}
