using System.Linq;
using Godot;

namespace SharpieStudio;

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
    }

    private void OnWindowCreated(Node node)
    {
        if (node is Window win)
        {
            win.CloseRequested += () => OnCloseRequested(win);
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

    public void AddAppIcon(Texture2D icon, string name)
    {
        var appIcon = AppIconScene.Instantiate<DesktopIcon>();
        appIcon.FileName = name;
        appIcon.IconTexture = icon;

        AddChild(appIcon);
    }

    private static void OnCloseRequested(Window win)
    {
        win.QueueFree();
    }
}
