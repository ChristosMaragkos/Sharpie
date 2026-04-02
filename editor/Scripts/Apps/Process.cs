namespace SharpieStudio.Apps;

public struct Process
{
    public TaskbarIcon TaskbarIcon { get; set; }
    public readonly WindowFrame? ActiveWinwow => TaskbarIcon.BoundWindow;
}
