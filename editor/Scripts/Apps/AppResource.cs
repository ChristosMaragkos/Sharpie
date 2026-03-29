using Godot;

namespace SharpieStudio.Apps;

[GlobalClass]
public partial class AppResource : Resource
{
    [Export]
    public string FileName;

    [Export]
    public string Description;

    [Export]
    public Texture2D Icon;

    [Export]
    public PackedScene AppScene;
}
