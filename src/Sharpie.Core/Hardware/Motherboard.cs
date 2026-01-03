using Sharpie.Core.Drivers;

namespace Sharpie.Core.Hardware;

public class Motherboard : IMotherboard
{
    private readonly Cpu _cpu;
    private readonly Ppu _ppu;
    private readonly Apu _apu;
    private readonly Memory _memory;
    private readonly Sequencer _sequencer;

    private float[] _audioBuffer = new float[4096];

    public byte FontColorIndex { get; private set; } = 1;
    private byte _fontSizeReg = 0;

    public byte[] ControllerStates { get; } = new byte[2];
    public byte[,] TextGrid { get; } = new byte[32, 32];

    private readonly IDisplayOutput _displayDevice;
    private readonly IAudioOutput _audioDevice;
    private readonly IInputHandler _inputDevice;

    private enum BiosFlagAddresses : ushort
    {
        MagicString = 0xFA20,
        Version = 0xFA24,
        CartridgeBootState = 0xFA27,
    }

    public Motherboard(IDisplayOutput display, IAudioOutput audio, IInputHandler input)
    {
        _memory = new Memory();
        _memory.FillRange(Memory.OamStart, 2048, 0xFF);
        _cpu = new Cpu(_memory, this);
        _ppu = new Ppu(_memory);
        _apu = new Apu(_memory);
        _apu.LoadDefaultInstruments();

        _sequencer = new Sequencer(_memory);
        for (int i = 0; i < 32; i++)
        for (int j = 0; j < 32; j++)
            TextGrid[i, j] = 0xFF;
        _cpu.LoadDefaultPalette();

        _displayDevice = display;
        _audioDevice = audio;
        _inputDevice = input;
        _cpu.Reset();
    }

    public void BootCartridge(Cartridge cart)
    {
        var bytesToLoad = Math.Min(cart.RomData.Length, Memory.OamStart); // capped to avoid any tomfoolery from manually edited files
        _memory.LoadData(Memory.RomStart, cart.RomData.Take(bytesToLoad).ToArray());

        _cpu.LoadPalette(cart.Palette);
        _cpu.Reset();
        Step();
    }

    public void BootCartridge(byte[] fileData)
    {
        var magic = System.Text.Encoding.ASCII.GetString(fileData, 0, 4);
        if (magic == null)
            throw new InvalidDataException("File is not large enough to be a Sharpie ROM.");
        if (magic != "SHRP")
            throw new FormatException("Not a valid Sharpie ROM: Invalid Header.");

        var romData = fileData.Skip(64).ToArray();
        _memory.LoadData(0, romData);
        // TODO: write first four bytes to MagicString, version to Version, and poll whether to load cartridge from CartridgeBootState
    }

    public void SetupDisplay()
    {
        _displayDevice.Initialize(256, "Sharpie");
    }

    public void SetupAudio()
    {
        _audioDevice.Initialize(44100);
    }

    public byte[] GetVideoBuffer()
    {
        return _ppu.GetFrame();
    }

    public float[] GetAudioBuffer()
    {
        return _audioBuffer;
    }

    public unsafe void UpdateAudio() => _apu.FillBuffer(_audioBuffer);

    public void VBlank()
    {
        GetInputState();
        _ppu.VBlank(this);
        _ppu.FlipBuffers();
    }

    public void ClearScreen(byte colorIndex)
    {
        for (int i = 0; i < 32; i++)
        for (int j = 0; j < 32; j++)
            TextGrid[i, j] = 0xFF;
        _ppu.BackgroundColorIndex = colorIndex;
    }

    public void DrawChar(int x, int y, byte charCode)
    {
        TextGrid[x, y] = charCode;
    }

    public void GetInputState()
    {
        var states = _inputDevice.GetInputState();

        if (states.Length == 0) // not sure who would ever do this
            return;
        ControllerStates[0] = states[0];

        if (states.Length == 1) // let's ensure no crashes
            return;
        ControllerStates[1] = states[1];
    }

    public void PlayNote(byte channel, byte note, byte instrument)
    {
        var freq = channel < 6 ? (440f * MathF.Pow(2f, (note - 69f) / 12f)) : note;
        var baseAddr = Memory.AudioRamStart + (channel * 4);

        _memory.WriteWord(baseAddr, (ushort)freq);
        _memory.WriteByte(baseAddr + 2, 0xFF);
        _memory.WriteByte(baseAddr + 3, (byte)((instrument << 1) | 1));
    }

    public void SetTextAttributes(byte attributes)
    {
        FontColorIndex = (byte)(attributes & 0x0F);
        _fontSizeReg = (byte)((attributes >> 4) & 0x0F);
    }

    public void StopChannel(byte channel)
    {
        var contolAddr = Memory.AudioRamStart + (channel * 4) + 3;
        var control = _memory.ReadByte(contolAddr);
        _memory.WriteByte(contolAddr, (byte)(control & (~0x01)));
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
        for (byte i = 0; i < 8; i++)
            StopChannel(i);
    }

    public void StartSequencer(ushort address)
    {
        _sequencer.LoadSong(address);
    }

    public ushort CheckCollision(int sprIdSrc)
    {
        var xSrc = _memory.ReadByte(Memory.OamStart + sprIdSrc);
        var ySrc = _memory.ReadByte(Memory.OamStart + sprIdSrc + 1);
        for (int i = 0; i < 2048; i += 4)
        {
            var sprId = _memory.ReadByte(Memory.OamStart + i + 2);
            if (sprId == sprIdSrc)
                continue; // don't check against self

            var x = _memory.ReadByte(Memory.OamStart + i);
            var y = _memory.ReadByte(Memory.OamStart + i + 1);
            var attr = _memory.ReadByte(Memory.OamStart + i + 3);

            if (x == 0xFF && y == 0xFF && sprId == 0xFF && attr == 0xFF)
                continue; // don't check blank oam slot

            if (Math.Abs(xSrc - x) >= 8 || Math.Abs(ySrc - y) >= 8)
                continue; // sprites can't touch

            return (ushort)i;
        }

        return 0xFFFF;
    }

    public void Step()
    {
        SetupDisplay();
        SetupAudio();
        for (var i = 0; i < 16000; i++)
        {
            if (_cpu.IsAwaitingVBlank)
                break;
            _cpu.Cycle();
        }

        VBlank();
        UpdateAudio();
        _sequencer.Step(this);
        _cpu.IsAwaitingVBlank = false;
    }
}
