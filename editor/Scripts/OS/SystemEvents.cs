using System;
using SharpieStudio.Apps;

namespace SharpieStudio.OS;

public static class SystemEvents
{
    public static event Action OnFileSystemChanged;

    public static void NotifyFileSystemChanged() => OnFileSystemChanged?.Invoke();

    public static event Action<AppResource, string[]> OnAppLaunchRequested;

    public static void RequestAppLaunch(AppResource app, params string[] args) =>
        OnAppLaunchRequested?.Invoke(app, args);
}
