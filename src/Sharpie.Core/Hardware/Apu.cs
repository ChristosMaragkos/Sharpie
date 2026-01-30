using Sharpie.Core.Drivers;

namespace Sharpie.Core.Hardware;

internal class Apu
{
    private IMotherboard _mobo;

    private float[] _phases = new float[8];

    private float[] _volumes = new float[8];
    private AdsrStage[] _stages = new AdsrStage[8];

    private ushort[] _lastFreq = new ushort[8];
    private byte[] _lastControl = new byte[8];

    private Random _noiseGen = new();
    private float[] _noiseValue = new float[8];
    private int[] _noiseTimer = new int[8];

    private bool[] _notePriority = new bool[8];

    private static int SequencerCounter = 0;

    public Apu(IMotherboard mobo)
    {
        _mobo = mobo;
    }

    internal void Reset()
    {
        SequencerCounter = 0;
        for (int i = 0; i < 8; i++)
        {
            _phases[i] = 0f;
            _volumes[i] = 0f;
            _stages[i] = AdsrStage.Idle;
            _lastFreq[i] = 0;
            _lastControl[i] = 0;
            _noiseValue[i] = 0f;
            _noiseTimer[i] = 0;
        }
    }

    private enum AdsrStage : byte
    {
        Idle, // silence
        Attack, // volume climbing to 1.0
        Decay, // falling toward sustain level
        Sustain, // stay in sustain level as long as gate bit is 1
        Release, // falling to 0.0
    }

    private readonly bool[] _retriggerQueued = new bool[8];

    public void RetriggerChannel(int channel)
    {
        _retriggerQueued[channel] = true;
    }

    private float GenerateSample(int channel)
    {
        var baseAddr = Memory.AudioRamStart + (channel * 4);

        var currentFreq = (float)_mobo.ReadWord(baseAddr);
        if (currentFreq == 0)
            return 0f;

        var currentControl = _mobo.ReadByte(baseAddr + 3);

        if (_retriggerQueued[channel])
        {
            _retriggerQueued[channel] = false;
            _stages[channel] = AdsrStage.Attack;
            _volumes[channel] *= 0.5f;
        }

        var gateWasOff = (_lastControl[channel] & 1) == 0;
        var gateIsOn = (currentControl & 1) != 0;
        var dataChanged =
            (currentFreq != _lastFreq[channel]) || (currentControl != _lastControl[channel]);

        if (gateIsOn && (gateWasOff || dataChanged))
        {
            _stages[channel] = AdsrStage.Attack;
            _volumes[channel] *= 0.5f;
        }

        _lastFreq[channel] = (ushort)currentFreq;
        _lastControl[channel] = currentControl;

        var volume = ProcessEnvelope(channel, currentControl);
        if (volume <= 0f && _stages[channel] == AdsrStage.Idle)
            return 0f;

        if (channel < 6)
        {
            var delta = currentFreq / 44100f;
            _phases[channel] += delta;
            if (_phases[channel] >= 1f)
                _phases[channel] -= 1f;

            return channel switch
                {
                    0 or 1 => Square(_phases[channel], delta),
                    2 or 3 => Triangle(_phases[channel]),
                    _ => Sawtooth(_phases[channel], delta),
                } * volume;
        }

        // --- NOISE CHANNELS (6-7) ---
        // We use a simple sample-counter.
        // We update the random value every 'N' samples.
        // If freq is Note 84 (Hi-Hat), we want N to be very small (Clear).
        // If freq is Note 20 (Bass Drum), we want N to be large (Crunchy).

        // This formula: Higher note = smaller period = faster noise.
        // Max note (127) gives period 1 (Pure Static).

        currentFreq *= 128f;
        var period = Math.Max(1, (127 - (int)currentFreq) / 4);

        _noiseTimer[channel]++;
        if (_noiseTimer[channel] >= period)
        {
            _noiseTimer[channel] = 0;
            _noiseValue[channel] = (_noiseGen.NextSingle() * 2f - 1f) * 0.3f;
        }

        return _noiseValue[channel] * volume * volume * 2f;
    }

    private float ProcessEnvelope(int channel, byte control)
    {
        var gateOn = (control & 0x01) != 0;
        var instrumentId = (control >> 1);
        var chanBaseAddr = Memory.AudioRamStart + (channel * 4);
        var chanMaxVolume = _mobo.ReadByte(chanBaseAddr + 2) / 255f;
        const float divisor = 100000f;

        var instr = _mobo.ReadInstrument(instrumentId);

        var aStep = (instr.Attack / divisor) + 0.000001f;
        var dStep = ((instr.Decay + 1) / divisor) + 0.000001f;
        var sLevel = instr.Sustain / 255f;
        var realSustain = sLevel * chanMaxVolume; // always a percentage of max volume
        var rStep = (instr.Release / divisor) + 0.000001f;

        if (!gateOn)
        {
            if (_stages[channel] != AdsrStage.Idle)
                _stages[channel] = AdsrStage.Release;
        }
        else
        {
            if (_stages[channel] == AdsrStage.Idle)
            {
                _stages[channel] = AdsrStage.Attack;
                _volumes[channel] = 0f;
            }
        }

        switch (_stages[channel])
        {
            case AdsrStage.Attack:
                _volumes[channel] += aStep;
                if (_volumes[channel] >= chanMaxVolume)
                    _stages[channel] = AdsrStage.Decay;
                break;
            case AdsrStage.Decay:
                _volumes[channel] -= dStep;
                if (_volumes[channel] <= realSustain)
                {
                    _volumes[channel] = realSustain;
                    _stages[channel] = AdsrStage.Sustain;
                }
                break;
            case AdsrStage.Sustain:
                _volumes[channel] = realSustain;
                break;
            case AdsrStage.Release:
                _volumes[channel] -= rStep;
                if (_volumes[channel] <= 0f)
                {
                    _volumes[channel] = 0f;
                    _stages[channel] = AdsrStage.Idle;
                }
                break;
            default:
                break;
        }

        return _volumes[channel];
    }

    private static float Sawtooth(float phase, float delta)
    {
        var initial = (phase * 2f - 1f);
        return initial - PolyBlep(phase, delta);
    }

    private static float Triangle(float phase)
    {
        var value = 0f;
        if (phase < 0.25f)
            value = phase * 4f;
        else if (phase < 0.75f)
            value = 2f - (phase * 4f);
        else
            value = (phase * 4f) - 4f;
        return value;
    }

    private static float Square(float phase, float delta)
    {
        var initial = (phase < 0.5f ? 1f : -1f);
        var correction = PolyBlep(phase, delta);
        var shift = (phase + 0.5f) % 1f;
        correction -= PolyBlep(shift, delta);
        return initial + correction;
    }

    public void FillBufferRange(float[] writeBuffer, int sampleCount = -1)
    {
        const float preGain = 0.3f;
        if (sampleCount < 0)
            sampleCount = writeBuffer.Length;

        if (!_isEnabled)
        {
            for (int i = 0; i < writeBuffer.Length; i++)
            {
                writeBuffer[i] = 0;
            }
            return;
        }

        for (var i = 0; i < sampleCount; i++)
        {
            AdvanceSequencer();

            var mixedSample = 0f;
            for (var chan = 0; chan < 8; chan++)
            {
                mixedSample += GenerateSample(chan);
            }
            writeBuffer[i] = MathF.Tanh(mixedSample * preGain);
        }
    }

    public unsafe void FillBufferRange(float* writeBuffer, uint sampleCount)
    {
        const float preGain = 0.3f;

        for (var i = 0; i < sampleCount; i++)
        {
            AdvanceSequencer();

            var mixedSample = 0f;
            for (var chan = 0; chan < 8; chan++)
                mixedSample += GenerateSample(chan);

            writeBuffer[i] = MathF.Tanh(mixedSample * preGain); // only YOU can prevent earrape!
        }
    }

    private static void AdvanceSequencer()
    {
        var AdvanceRate = 1024 / Sequencer.TempoMultiplier;

        if (++SequencerCounter >= AdvanceRate)
        {
            SequencerCounter -= AdvanceRate;
            Sequencer.Instance?.Step();
        }
    }

    internal void ResetPhase(int channel)
    {
        if (channel >= 0 && channel < _phases.Length)
        {
            _phases[channel] = 0f;
            _stages[channel] = AdsrStage.Idle;
        }
    }

    internal void ClearPhases()
    {
        Array.Clear(_phases);
    }

    internal void LoadDefaultInstruments()
    {
        byte[][] defaults = new byte[][]
        {
            // 0: Fast Attack, Full Sustain, Short Release
            new byte[] { 0x0F, 0x00, 0xFF, 0x05 },
            // 1: Soft Attack, Med Decay, Med Sustain
            new byte[] { 0x05, 0x10, 0xAA, 0x10 },
            // 2: Slow Attack, Long Release
            new byte[] { 0x02, 0x05, 0x88, 0x40 },
            // 3: Instant Attack, Fast Decay, No Sustain
            new byte[] { 0xF0, 0x20, 0x00, 0xF0 },
        };

        for (var i = 0; i < 4; i++)
        {
            var instr = defaults[i];
            _mobo.DefineInstrument(i, instr[0], instr[1], instr[2], instr[3]);
        }
    }

    private static float PolyBlep(float phase, float delta)
    {
        if (phase < delta) // are we at the start of the way?
        {
            phase /= delta;
            return (phase + phase - phase * phase - 1f); // phase phase phase phase phase phase
        }
        else if (phase > 1f - delta) // are we at the end?
        {
            phase = (phase - 1f) / delta;
            return (phase + phase + phase * phase + 1f);
        }

        return 0f; // no need to smooth non edge values
    }

    private bool _isEnabled;

    internal void Enable()
    {
        _isEnabled = true;
    }

    internal void Disable()
    {
        _isEnabled = false;
    }

    public void SetNotePriority(int channel, bool value) => _notePriority[channel] = value;

    public bool IsCurrentNotePrioritized(int channel) => _notePriority[channel];
}
