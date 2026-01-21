using System.Collections.Concurrent;

namespace Sharpie.Core.Drivers;

public abstract class DebugOutput
{
    protected ConcurrentQueue<string> MessageQueue = new();
    private readonly int _size;

    protected DebugOutput(int size)
    {
        _size = size;
    }

    public void PushDebug(string message)
    {
        if (MessageQueue.Count == _size)
            MessageQueue.TryDequeue(out _);

        MessageQueue.Enqueue(message);
    }

    public void LogAll()
    {
        while (!MessageQueue.IsEmpty)
        {
            MessageQueue.TryDequeue(out string? message);
            if (message != null)
                Log(message);
        }
    }

    public abstract void Log(string message);
}
