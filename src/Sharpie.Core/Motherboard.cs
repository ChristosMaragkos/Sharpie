namespace Sharpie.Core;

public class Motherboard : IMotherboard
{
    private readonly Cpu _cpu;
    private readonly Memory _memory;

    public Motherboard()
    {
        _memory = new Memory();
        _cpu = new Cpu(_memory, this);
    }

    public void LoadRom(byte[] rom) => _memory.LoadData(0, rom);

    public void AwaitVBlank()
    {
        throw new NotImplementedException();
    }

    public void ClearScreen(byte colorIndex)
    {
        throw new NotImplementedException();
    }

    public void DrawChar(ushort x, ushort y, ushort charCode)
    {
        throw new NotImplementedException();
    }

    public ushort GetInputState(byte controllerIndex)
    {
        throw new NotImplementedException();
    }

    public void PlayNote(byte channel, byte note)
    {
        throw new NotImplementedException();
    }

    public void SetTextAttributes(byte attributes)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public void Run()
    {
        _cpu.Cycle();
    }
}
