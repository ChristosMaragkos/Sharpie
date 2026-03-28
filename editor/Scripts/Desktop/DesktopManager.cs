using System.Linq;
using Godot;
using SharpieStudio.Apps;

namespace SharpieStudio.Desktop;

public partial class DesktopManager : Node
{
    private static readonly PackedScene WindowScene = GD.Load<PackedScene>(
        "res://Scenes/window.tscn"
    );

    private static readonly PackedScene AppIconScene = GD.Load<PackedScene>(
        "res://Scenes/desktop_icon.tscn"
    );

    public override void _Ready()
    {
        ChildEnteredTree += OnWindowCreated;

        foreach (var win in GetChildren().OfType<Window>())
        {
            win.CloseRequested += () => OnCloseRequested(win);
        }

        using var dirAccess = DirAccess.Open("res://Resources/");

        foreach (
            var resourceFile in dirAccess
                .GetFiles()
                .Where(fp => fp.EndsWith(".tres") || fp.EndsWith(".res"))
                .Select(fp => dirAccess.GetCurrentDir() + "/" + fp)
        )
        {
            GD.Print(resourceFile);
            var appData = GD.Load<AppResource>(resourceFile);
            AddAppIcon(appData);
        }

        dirAccess.Dispose();
    }

    private void OnWindowCreated(Node node)
    {
        if (node is Window win)
        {
            win.CloseRequested += () => OnCloseRequested(win);
            win.FocusEntered += this.UnfocusAppIcons;
        }
    }

    public override void _Process(double delta) { }

    public void AddWindow(PackedScene appContent, string title)
    {
        var window = WindowScene.Instantiate<Window>();
        window.Title = title;

        var windowContent = appContent.Instantiate();
        window.AddChild(windowContent);
        AddChild(window);
    }

    public void AddAppIcon(AppResource appResource)
    {
        var appIcon = AppIconScene.Instantiate<DesktopIcon>();
        appIcon.Data = appResource;

        AddChild(appIcon);
    }

    private static void OnCloseRequested(Window win)
    {
        win.QueueFree();
    }
}
