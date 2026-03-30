using System.Collections.Generic;
using System.Linq;
using Godot;
using SharpieStudio.Apps;

namespace SharpieStudio.Desktop;

public partial class DesktopManager : Node
{
    private const string DesktopBackgroundPath = "VBoxContainer/Desktop";
    private const string QuickLaunchAreaPath = "VBoxContainer/TaskBar/HBoxContainer/WindowList";
    public const string ActiveWindowGroup = "ActiveWindow";

    private static readonly PackedScene WindowScene = GD.Load<PackedScene>(
        "res://Scenes/window_frame.tscn"
    );

    private static readonly PackedScene DesktopIconScene = GD.Load<PackedScene>(
        "res://Scenes/desktop_icon.tscn"
    );

    private static readonly PackedScene TaskbarIconScene = GD.Load<PackedScene>(
        "res://Scenes/taskbar_icon.tscn"
    );

    private readonly Dictionary<string, WindowFrame> OpenWindows = [];

    public override void _Ready()
    {
        ChildEnteredTree += OnWindowCreated;

        foreach (var win in GetChildren().OfType<WindowFrame>())
        {
            win.OnCloseRequested += OnCloseRequested;
        }

        LoadApps();
    }

    private void LoadApps()
    {
        using var dirAccess = DirAccess.Open("res://Resources/Apps");

        foreach (
            var resourceFile in dirAccess
                .GetFiles()
                .Where(fp => fp.EndsWith(".tres") || fp.EndsWith(".res"))
                .Select(fp => dirAccess.GetCurrentDir() + "/" + fp)
        )
        {
            var appData = GD.Load<AppResource>(resourceFile);
            AddAppIcon(appData);
        }

        dirAccess.Dispose();
    }

    private void OnWindowCreated(Node node)
    {
        if (node is WindowFrame win)
        {
            win.OnCloseRequested += OnCloseRequested;
            win.FocusEntered += this.UnfocusAppIcons;
        }
    }

    public void TryOpenWindow(AppResource data)
    {
        foreach (var window in OpenWindows.Values)
        {
            window.RemoveFromGroup(ActiveWindowGroup);
        }

        if (OpenWindows.TryGetValue(data.FileName, out var open))
        {
            open.BringToFront();
            return;
        }

        AddWindow(data);
    }

    public void AddWindow(AppResource data)
    {
        this.UnfocusAppIcons();
        var window = WindowScene.Instantiate<WindowFrame>();

        window.Configure(data);
        window.OnCloseRequested += OnCloseRequested;
        GetNode(DesktopBackgroundPath).AddChild(window);

        OpenWindows[data.FileName] = window;
    }

    public void AddAppIcon(AppResource appResource)
    {
        var appIcon = DesktopIconScene.Instantiate<DesktopIcon>();
        appIcon.Configure(appResource);
        GetNode(DesktopBackgroundPath).AddChild(appIcon);
        appIcon.OpenRequested += TryOpenWindow;

        var taskbarIcon = TaskbarIconScene.Instantiate<TaskbarIcon>();
        taskbarIcon.Configure(appResource);
        GetNode(QuickLaunchAreaPath).AddChild(taskbarIcon);
        taskbarIcon.OpenRequested += TryOpenWindow;
    }

    private void OnCloseRequested(WindowFrame win)
    {
        OpenWindows.Remove(win.TitleLabel.Text);
        win.QueueFree();
    }
}
