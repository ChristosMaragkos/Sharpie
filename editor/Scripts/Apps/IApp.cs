using Godot;

namespace SharpieStudio.Apps;

internal interface IApp
{
    public WindowFrame? GetEnclosingWindow()
    {
        if (this is not Node node)
            return null;

        return node.GetParent().GetParent().GetParent<WindowFrame>() ?? null;
    }
}
