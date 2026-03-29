using System.Collections.Generic;
using System.Linq;
using Godot;
using SharpieStudio.Apps;

namespace SharpieStudio.Desktop;

public partial class DesktopManager : Node
{
    private const string DesktopBackgroundPath = "VBoxContainer/Desktop";

    private static readonly PackedScene WindowScene = GD.Load<PackedScene>(
        "res://Scenes/window_frame.tscn"
    );

    private static readonly PackedScene AppIconScene = GD.Load<PackedScene>(
        "res://Scenes/desktop_icon.tscn"
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
        if (OpenWindows.TryGetValue(data.FileName, out var open))
        {
            open.MoveToFront();
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
        var appIcon = AppIconScene.Instantiate<DesktopIcon>();
        appIcon.Data = appResource;

        GetNode(DesktopBackgroundPath).AddChild(appIcon);
        appIcon.OpenRequested += TryOpenWindow;
    }

    private void OnCloseRequested(WindowFrame win)
    {
        OpenWindows.Remove(win.TitleLabel.Text);
        win.QueueFree();
    }
}
