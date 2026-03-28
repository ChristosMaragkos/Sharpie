using Godot;

namespace SharpieStudio;

public static class DesktopItemExtensions
{
    extension(Node node)
    {
        public void MoveToFront() => node.GetParent()?.MoveChild(node, -1);
    }
}
