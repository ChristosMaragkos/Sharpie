using Godot;

namespace SharpieStudio;

public partial class DesktopManager : Node
{
    public override void _Ready()
    {
        ChildEnteredTree += OnWindowCreated;
    }

    private void OnWindowCreated(Node node)
    {
        if (node is Window win)
        {
            win.CloseRequested += () => OnCloseRequested(win);
        }
    }

    public override void _Process(double delta) { }

    public void AddWindow(PackedScene scene, string title)
    {
        var window = scene.Instantiate<Window>();
        if (window is not null)
            window.Title = title;

        AddChild(window);
    }

    private static void OnCloseRequested(Window win)
    {
        win.CloseRequested -= () => OnCloseRequested(win);
        win.QueueFree();
    }
}
