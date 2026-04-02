using System;

namespace SharpieStudio.Apps;

public struct Process
{
    public TaskbarIcon TaskbarIcon { get; set; }
    public readonly WindowFrame? ActiveWinwow => TaskbarIcon.BoundWindow;
    public Guid Id { get; set; } = new Guid();

    public Process(TaskbarIcon icon)
    {
        TaskbarIcon = icon;
    }
}
