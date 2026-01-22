using Sharpie.Core.Hardware;

namespace Sharpie.Core.Drivers;

internal class Sequencer
{
    private readonly IMotherboard _mobo;
    public int Cursor { get; set; } = 0;
    private int _delayFrames = 0;
    public bool Enabled { get; set; } = false;

    public static int TempoMultiplier { get; private set; } = 1;

    public Sequencer(IMotherboard mobo)
    {
        _mobo = mobo;
        if (Instance == null)
            Instance = this;
    }

    public void Reset()
    {
        Enabled = false;
        Cursor = 0;
        _delayFrames = 0;
    }

    public static Sequencer? Instance { get; private set; }

    public void LoadSong(int startAddr)
    {
        Cursor = startAddr;
        _delayFrames = 0;
        Enabled = true;
    }

    public void Step()
    {
        if (!Enabled)
            return;
        if (_delayFrames > 0)
        {
            _delayFrames--;
            return;
        }

        while (Enabled && _delayFrames == 0)
        {
            var channel = _mobo.ReadByte(Cursor);
            var note = _mobo.ReadByte(Cursor + 1);
            var duration = _mobo.ReadByte(Cursor + 2);
            var instrument = _mobo.ReadByte(Cursor + 3);

            if (channel == 0xFF) // END
            {
                Enabled = false;
                _mobo.StopAllSounds();
                TempoMultiplier = 1;
                break;
            }
            else if (channel == 0xFE) // GOTO
            {
                var stepsBack = (ushort)(duration | (instrument << 8));
                Cursor -= 4 * stepsBack; // move the cursor 4 bytes back per step (since each packet is four bytes)
                continue;
            }
            else if (channel == 0xFD) // TEMPO
            {
                TempoMultiplier = duration;
                Cursor += 4;
                continue;
            }
            else if (note == 0)
            {
                _mobo.StopChannel(channel);
            }
            else
            {
                _mobo.PlayNote(channel, note, instrument);
            }

            _delayFrames = duration;
            Cursor += 4;
        }
    }
}
