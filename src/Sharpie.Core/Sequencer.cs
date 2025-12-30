namespace Sharpie.Core;

public class Sequencer
{
    private readonly Memory _memory;
    private int _cursor = 0;
    private int _delayFrames = 0;
    public bool Enabled { get; set; } = false;

    public Sequencer(Memory memory)
    {
        _memory = memory;
    }

    public void LoadSong(int startAddr)
    {
        _cursor = startAddr;
        _delayFrames = 0;
        Enabled = true;
    }

    public void Step(IMotherboard mobo)
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
            var channel = _memory.ReadByte(_cursor);
            var note = _memory.ReadByte(_cursor + 1);
            var duration = _memory.ReadByte(_cursor + 2);
            var instrument = _memory.ReadByte(_cursor + 3);

            if (channel == 0xFF) // END
            {
                Enabled = false;
                mobo.StopAllSounds();
                break;
            }
            else if (channel == 0xFE) // GOTO
            {
                var jumpAddr = (ushort)(duration | (instrument << 8));
                _cursor = jumpAddr;
                continue;
            }
            else if (note == 0)
            {
                mobo.StopChannel(channel);
            }
            else
            {
                mobo.PlayNote(channel, note, instrument);
            }

            _delayFrames = duration;
            _cursor += 4;
        }
    }
}
