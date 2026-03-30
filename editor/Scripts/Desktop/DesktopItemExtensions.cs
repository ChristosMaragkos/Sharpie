using Godot;

namespace SharpieStudio.Desktop;

public static class DesktopItemExtensions
{
    extension(Node node)
    {
        public void UnfocusAppIcons() =>
            node.GetTree().CallGroup("DesktopIcons", "SetSelected", false);
    }
}
