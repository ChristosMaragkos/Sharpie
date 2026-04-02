using System;

namespace SharpieStudio.OS;

public static class SystemEvents
{
    public static event Action OnFileSystemChanged;

    public static void NotifyFileSystemChanged() => OnFileSystemChanged?.Invoke();
}
