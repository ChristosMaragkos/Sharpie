using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Raylib_cs;
using Sharpie.Core.Drivers;

namespace Sharpie.Runner.RaylibCs.Impl;

public class RaylibAudioOutput : IAudioOutput
{
    private AudioStream _stream;
    private static int SequencerCounter = 0;
    private static int SampleRate = 44100;
    private const int TargetFPS = 60;
    
    // Calculate samples per frame dynamically based on sample rate
    private static int SamplesPerFrame => SampleRate / TargetFPS;

    public unsafe void Initialize(int sampleRate)
    {
        SampleRate = sampleRate;
        Raylib.InitAudioDevice();
        
        // Reduce buffer size for more responsive audio and less latency
        // 2048 samples at 44100 Hz = ~46ms latency (vs 93ms with 4096)
        Raylib.SetAudioStreamBufferSizeDefault(2048);
        
        // Use 32-bit float, mono channel
        _stream = Raylib.LoadAudioStream((uint)sampleRate, 32, 1);
        Raylib.SetAudioStreamCallback(_stream, &AudioCallback);
        Raylib.PlayAudioStream(_stream);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void AudioCallback(void* buffer, uint frames)
    {
        if (Sharpie.Core.Hardware.Apu.Instance == null) // just in case it's still uninitialized
            return;

        float* floatBuffer = (float*)buffer;
        Sharpie.Core.Hardware.Apu.Instance.FillBufferRange(floatBuffer, frames);

        // Update sequencer based on actual samples processed
        // This ensures correct tempo regardless of callback frequency
        SequencerCounter += (int)frames;
        while (SequencerCounter >= SamplesPerFrame)
        {
            SequencerCounter -= SamplesPerFrame;
            Sequencer.Instance?.Step();
        }
    }

    public void HandleAudioBuffer(float[] audioBuffer) { }

    public void Cleanup()
    {
        Raylib.UnloadAudioStream(_stream);
        Raylib.CloseAudioDevice();
    }
}
