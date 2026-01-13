public class RaylibDebugOutpug : DebugOutput
{
    public RaylibDebugOutpug(int size)
        : base(size) { }

    public override void Log(string message)
    {
        System.Console.WriteLine(message);
    }
}
