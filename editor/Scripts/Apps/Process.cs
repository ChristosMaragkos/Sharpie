namespace SharpieStudio.Apps;

public struct Process
{
    public DesktopIcon DesktopIcon { get; set; }
    public TaskbarIcon TaskbarIcon { get; set; }
    public readonly WindowFrame? ActiveWinwow => TaskbarIcon.BoundWindow;
}
