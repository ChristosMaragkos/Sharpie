using Sharpie.Core.Drivers;
using SpriteFlags = Sharpie.Core.Hardware.OamBank.SpriteFlags;

namespace Sharpie.Core.Hardware;

internal class Motherboard : IMotherboard
{
    private readonly Cpu _cpu;
    private readonly Ppu _ppu;
    private static Apu? Apu { get; set; }
    private readonly Memory _ram;
    private readonly Memory _biosRom;
    private readonly OamBank _oam;
    private readonly Sequencer _sequencer;

    public bool IsInBootMode { get; private set; }
    private const ushort ReservedSpaceStart = Memory.ReservedSpaceStart;
    private byte[] _cartPalette = new byte[16];

    public byte FontColorIndex { get; private set; } = 1;

    public byte[] ControllerStates { get; } = new byte[2];
    public byte[,] TextGrid { get; } = new byte[32, 32];

    private readonly IDisplayOutput _displayDevice;
    private readonly IAudioOutput _audioDevice;
    private readonly InputHandler _inputDevice;
    private readonly DebugOutput? _dbg;

    private bool _isPoweringOn = true;

    private enum BiosFlagAddresses : ushort
    {
        MagicString = 0xFA20,
        Version = 0xFA24,
        CartVerificationState = 0xFA26,
        IsCartLoaded = 0xFA28,
        ErrorCode = 0xFA29,
    }

    public Motherboard(
        IDisplayOutput display,
        IAudioOutput audio,
        InputHandler input,
        DebugOutput? dbg = null
    )
    {
        _ram = new Memory();
        _biosRom = new Memory();
        _oam = new OamBank();
        ResetOam();

        _cpu = new Cpu(this);
        _cpu.LoadDefaultPalette();
        _cpu.RequestReset();

        _ppu = new Ppu(this);

        Apu = new Apu(this);
        Apu.LoadDefaultInstruments();

        _sequencer = new Sequencer(this);

        for (int i = 0; i < 32; i++)
        for (int j = 0; j < 32; j++)
            TextGrid[i, j] = 0xFF;

        _displayDevice = display;
        _audioDevice = audio;
        _inputDevice = input;
        _dbg = dbg;
        SetupDisplay();
        SetupAudio();
        IsInBootMode = true;
        _isPoweringOn = false;
    }

    private void ResetOam()
    {
        _oam.InvalidateAll();
    }

    public byte ReadByte(ushort address)
    {
        if (IsInBootMode && address <= Memory.SpriteAtlasStart)
        {
            return _biosRom.ReadByte(address);
        }

        return _ram.ReadByte(address);
    }

    public byte ReadByte(int address) => ReadByte((ushort)address);

    public ushort ReadWord(ushort address)
    {
        if (IsInBootMode && address <= Memory.SpriteAtlasStart)
            return _biosRom.ReadWord(address);

        return _ram.ReadWord(address);
    }

    public ushort ReadWord(int address) => ReadWord((ushort)address);

    public void WriteByte(ushort address, byte value)
    {
        if (
            address >= Memory.ReservedSpaceStart
            && address < Memory.ColorPaletteStart
            && !IsInBootMode
        )
        {
            TriggerSegfault(SegfaultType.ReservedRegionWrite);
            return;
        }

        _ram.WriteByte(address, value);

        if (address != (ushort)BiosFlagAddresses.CartVerificationState || !IsInBootMode)
            return;

        if (value == 0x01)
        {
            BootIntoCartridge();
        }
    }

    public void WriteByte(int address, byte value) => WriteByte((ushort)address, value);

    public void WriteWord(ushort address, ushort value)
    {
        if (
            address >= Memory.ReservedSpaceStart
            && address <= Memory.ColorPaletteStart
            && !IsInBootMode
        )
        {
            TriggerSegfault(SegfaultType.ReservedRegionWrite);
            return;
        }

        _ram.WriteWord(address, value);

        if (address != (ushort)BiosFlagAddresses.CartVerificationState || !IsInBootMode)
            return;

        if (value == 0x01) // cart is ok, swap RAM banks
        {
            BootIntoCartridge();
        }
    }

    public void WriteWord(int address, ushort value) => WriteWord((ushort)address, value);

    public void FillRange(int startIndex, int amount, byte value)
    {
        _ram.FillRange(startIndex, amount, value);
    }

    public void LoadBios(byte[] biosData)
    {
        _biosRom.LoadData(0, biosData);
        _ram.WriteByte((ushort)BiosFlagAddresses.IsCartLoaded, 0x00); // no cart loaded
    }

    public void LoadCartridge(byte[] fileData)
    {
        try
        {
            var header = fileData.Take(64).ToArray();
            for (int i = 0; i < 4; i++)
                _ram.WriteByte((ushort)BiosFlagAddresses.MagicString + i, header[i]);

            _ram.WriteByte((ushort)BiosFlagAddresses.Version, header[42]); // header memory addresses 42 and 43 is where the
            _ram.WriteByte((ushort)BiosFlagAddresses.Version + 1, header[43]); // minimum BIOS version required to run the cartridge lives

            _cartPalette = header.Skip(48).ToArray();

            var cartData = fileData.Skip(64).ToArray();
            _ram.LoadData(0, cartData);
            _ram.WriteByte((ushort)BiosFlagAddresses.IsCartLoaded, 0x01); // yes cart loaded
        }
        catch
        {
            _ram.WriteByte((ushort)BiosFlagAddresses.IsCartLoaded, 0xFF); // not a rom
        }
    }

    public void TriggerSegfault(SegfaultType segfaultType)
    {
        if (_isPoweringOn)
            return;

        PushDebug(segfaultType.GetMessage());
        _ram.WriteByte((ushort)BiosFlagAddresses.ErrorCode, (byte)segfaultType);
        ResetState();
    }

    private void ResetState()
    {
        _cpu.Halt();
        _ram.ClearRange(0, Memory.SpriteAtlasStart);
        _sequencer.Reset();
        StopAllSounds();
        Apu?.Disable();
        Apu?.Reset();
        Apu?.LoadDefaultInstruments();
        Apu?.Enable();
        ResetOam();
        ClearTextGrid();
        IsInBootMode = true;
        FontColorIndex = 1;
        _cpu.RequestReset();
    }

    private void BootIntoCartridge()
    {
        _cpu.Halt();
        FontColorIndex = 1;
        IsInBootMode = false;
        ResetOam();
        StopAllSounds();
        Apu?.Disable();
        Apu?.Reset();
        Apu?.LoadDefaultInstruments();
        Apu?.Enable();
        _sequencer.Reset();
        _cpu.RequestReset();
        _cpu.LoadPalette(_cartPalette);
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

    public void VBlank()
    {
        GetInputState();
        _ppu.VBlank(_oam);
    }

    public void ClearScreen(byte colorIndex)
    {
        ClearTextGrid();
        _ppu.BackgroundColorIndex = colorIndex;
        _oam.Invalidate(_oam.Cursor * 6, OamBank.Size - 1);
    }

    private void ClearTextGrid()
    {
        for (int i = 0; i < 32; i++)
        for (int j = 0; j < 32; j++)
            TextGrid[i, j] = 0xFF;
    }

    public void DrawChar(int x, int y, byte charCode)
    {
        TextGrid[x, y] = charCode;
    }

    public void GetInputState()
    {
        var states = _inputDevice.GetInputState();
        (ControllerStates[0], ControllerStates[1]) = (states.Item1, states.Item2);
    }

    public void PlayNote(
        byte channel,
        byte note,
        byte instrument,
        bool priority = false,
        bool allowOverride = false
    )
    {
        if (!Apu!.IsCurrentNotePrioritized(channel) || allowOverride)
        {
            Apu.SetNotePriority(channel, priority);

            var freq = channel < 6 ? (440f * MathF.Pow(2f, (note - 69f) / 12f)) : note;
            var baseAddr = Memory.AudioRamStart + (channel * 4);

            var control = _ram.ReadByte(baseAddr + 3) & 1;
            if (control == 1) // gate is on, retrigger the envelope
                Apu?.RetriggerChannel(channel);

            _ram.WriteWord(baseAddr, (ushort)freq);
            _ram.WriteByte(baseAddr + 2, 0xFF);
            _ram.WriteByte(baseAddr + 3, (byte)((instrument << 1) | 1));
        }
    }

    public void SetTextAttributes(byte attributes)
    {
        FontColorIndex = (byte)(attributes & 0x0F);
    }

    public void StopChannel(byte channel)
    {
        var contolAddr = Memory.AudioRamStart + (channel * 4) + 3;
        var control = _ram.ReadByte(contolAddr);
        _ram.WriteByte(contolAddr, (byte)(control & (~0x01)));
        Apu?.SetNotePriority(channel, false);
    }

    public void StopSystem()
    {
        _cpu.Halt();
    }

    public void SwapColor(byte oldIndex, byte newIndex)
    {
        _ram.WriteByte(Memory.ColorPaletteStart + oldIndex, newIndex);
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

    public ushort CheckCollision(int srcIndex)
    {
        var (x, y, id, attr, type) = _oam.ReadEntry(srcIndex);

        for (var i = 0; i < OamBank.MaxEntries; i++)
        {
            if (i == srcIndex)
                continue;

            var (targX, targY, targId, targAttr, targType) = _oam.ReadEntry(i);

            if (
                (targAttr & (ushort)SpriteFlags.Background) != 0
                || (targAttr & (ushort)SpriteFlags.Hud) != 0
            )
                continue;

            // if this seems unintuitive, it's because it's actual math. Don't worry about it.
            if (x < targX + 8 && x + 8 > targX && y < targY + 8 && y + 8 > targY)
            {
                return (ushort)i;
            }
        }
        return 0xFFFF; // no collision
    }

    public void PushDebug(string message)
    {
        _dbg?.PushDebug(message);
    }

    public static unsafe void FillAudioBufferRange(float* audioBuffer, uint sampleAmount) =>
        Apu?.FillBufferRange(audioBuffer, sampleAmount);

    public static void FillAudioBufferRange(float[] audioBuffer, int sampleAmount) =>
        Apu?.FillBufferRange(audioBuffer, sampleAmount);

    public void ToggleSequencer() => _sequencer.Enabled = !_sequencer.Enabled;

    public int GetOamCursor() => _oam.Cursor;

    public void SetOamCursor(int value)
    {
        if (value >= OamBank.MaxEntries)
        {
            TriggerSegfault(SegfaultType.OamCursorOutOfBounds);
            return;
        }
        _oam.Cursor = value;
    }

    public ushort GetSequencerCursor() => (ushort)_sequencer.Cursor;

    public void SetSequencerCursor(ushort value) => _sequencer.Cursor = value;

    public void WriteSpriteEntry(ushort x, ushort y, byte tileId, byte attr, byte type) =>
        _oam.WriteEntry(x, y, tileId, attr, type);

    public (ushort X, ushort Y, byte TileId, byte Attr, byte Type) ReadSpriteEntry(int index) =>
        _oam.ReadEntry(index);

    public void MoveCamera(int dx, int dy)
    {
        _ppu.CamX = (ushort)Math.Clamp((int)_ppu.CamX + dx, 0, ushort.MaxValue);
        _ppu.CamY = (ushort)Math.Clamp((int)_ppu.CamY + dy, 0, ushort.MaxValue);
    }

    public void SetCamera(ushort x, ushort y)
    {
        _ppu.CamX = x;
        _ppu.CamY = y;
    }

    public void Step()
    {
        for (var i = 0; i < 16000; i++)
        {
            if (_cpu.IsAwaitingVBlank || _cpu.IsHalted)
                break;
            _cpu.Cycle();
        }

        VBlank();
        _ppu.FlipBuffers();
        _cpu.IsAwaitingVBlank = false;
    }
}
