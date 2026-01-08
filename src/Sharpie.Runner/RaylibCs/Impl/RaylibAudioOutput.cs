using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Raylib_cs;
using Sharpie.Core.Drivers;
using Sharpie.Core.Hardware;

namespace Sharpie.Runner.RaylibCs.Impl;

public class RaylibAudioOutput : IAudioOutput
{
    private AudioStream _stream;

    public unsafe void Initialize(int sampleRate)
    {
        Raylib.InitAudioDevice();
        Raylib.SetAudioStreamBufferSizeDefault(512);
        _stream = Raylib.LoadAudioStream((uint)sampleRate, 32, 1);
        Raylib.SetAudioStreamCallback(_stream, &AudioCallback);
        Raylib.PlayAudioStream(_stream);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void AudioCallback(void* buffer, uint frames)
    {
        float* floatBuffer = (float*)buffer;
        Motherboard.FillAudioBufferRange(floatBuffer, frames);
    }

    public void HandleAudioBuffer(float[] audioBuffer) { }

    public void Cleanup()
    {
        Raylib.UnloadAudioStream(_stream);
        Raylib.CloseAudioDevice();
    }
}
