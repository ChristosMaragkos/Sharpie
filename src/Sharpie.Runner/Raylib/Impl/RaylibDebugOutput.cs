public class RaylibDebugOutpug : DebugOutput
{
    public RaylibDebugOutpug(int size)
        : base(size) { }

    public override void Log(string message)
    {
        Console.WriteLine(message);
    }
}
