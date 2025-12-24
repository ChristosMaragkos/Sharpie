namespace Sharpie.Core;

public class Apu
{
    private readonly Memory _ram;
    private readonly float[] _phases = new float[8]; // current phase for every oscillator
    private readonly float[] _volumes = new float[8];
    private readonly AdsrStage[] _stages = new AdsrStage[8];
    private readonly Random _noise = new();

    public Apu(Memory ram)
    {
        _ram = ram;
    }

    private enum AdsrStage : byte
    {
        Idle, // silence
        Attack, // volume climbing to 1.0
        Decay, // falling toward sustain level
        Sustain, // stay in sustain level as long as gate bit is 1
        Release, // falling to 0.0
    }

    private float GenerateSample(int channel)
    {
        var baseAddr = Memory.AudioRamStart + (channel * 4);
        var freq = _ram.ReadWord(baseAddr);
        var control = _ram.ReadByte(baseAddr + 3);

        if (freq == 0)
            return 0f;
        var volume = ProcessEnvelope(channel, control);
        if (volume <= 0f && _stages[channel] == AdsrStage.Idle)
            return 0f;

        _phases[channel] += freq / 44100f;
        if (_phases[channel] >= 1f)
            _phases[channel] -= 1f;

        var wave = channel switch
        {
            0 or 1 => Square(_phases[channel]),
            2 or 3 => Triangle(_phases[channel]),
            4 or 5 => Sawtooth(_phases[channel]),
            _ => Noise(),
        };

        return wave * volume;
    }

    private float ProcessEnvelope(int channel, byte control)
    {
        var gateOn = (control & 0x01) != 0;
        var instrumentId = (control >> 1);
        var instrumentAddr = Memory.AudioRamStart + 32 + (instrumentId * 4);
        var chanBaseAddr = Memory.AudioRamStart + (channel * 4);
        var chanMaxVolume = _ram.ReadByte(chanBaseAddr + 2) / 255f;
        const float divisor = 500000f;

        var aStep = (_ram.ReadByte(instrumentAddr) / divisor) + 0.000001f;
        var dStep = (_ram.ReadByte(instrumentAddr + 1) / divisor) + 0.000001f;
        var sLevel = _ram.ReadByte(instrumentAddr + 2) / 255f;
        var realSustain = sLevel * chanMaxVolume; // always a percentage of max volume
        var rStep = (_ram.ReadByte(instrumentAddr + 3) / divisor) + 0.000001f;

        if (!gateOn && _stages[channel] != AdsrStage.Idle)
            _stages[channel] = AdsrStage.Release;
        else if (gateOn && _stages[channel] is AdsrStage.Idle or AdsrStage.Release)
            _stages[channel] = AdsrStage.Attack;

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
                    _stages[channel] = AdsrStage.Sustain;
                if (_volumes[channel] <= 0f && sLevel == 0f)
                {
                    _stages[channel] = AdsrStage.Idle;
                    _ram.WriteByte(chanBaseAddr + 3, (byte)(control & 0xFE)); // force the gate to 0 to stop note from looping
                }
                break;
            case AdsrStage.Sustain:
                _volumes[channel] = realSustain;
                break;
            case AdsrStage.Release:
                _volumes[channel] -= rStep;
                if (_volumes[channel] <= 0f)
                    _stages[channel] = AdsrStage.Idle;
                break;
            default:
                break;
        }

        return _volumes[channel];
    }

    private float Noise()
    {
        return (_noise.NextSingle() * 2f - 1f) * 1.2f;
    }

    private static float Sawtooth(float phase)
    {
        return (phase * 2f - 1f) * 0.6f;
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
        return value * 1.4f;
    }

    private static float Square(float phase)
    {
        return (phase < 0.5f ? 1f : -1f) * 0.4f;
    }

    internal void FillBuffer(float[] writeBuffer)
    {
        for (var i = 0; i < writeBuffer.Length; i++)
        {
            var mixedSample = 0f;
            var activeChannels = 0;
            for (var chan = 0; chan < 8; chan++)
            {
                var sample = GenerateSample(chan);
                if (MathF.Abs(sample) > 0)
                    activeChannels++;
                mixedSample += sample;
            }
            var divisor = MathF.Max(1.5f, activeChannels * 0.5f);
            writeBuffer[i] = (mixedSample / divisor) * 0.5f; // normalized with AGC to avoid earrape
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
        Array.Clear(_phases);
    }
}
