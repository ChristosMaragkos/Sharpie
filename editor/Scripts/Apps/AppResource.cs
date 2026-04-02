using Godot;

namespace SharpieStudio.Apps;

[GlobalClass]
public partial class AppResource : Resource
{
    [Export]
    public string AppName;

    [Export]
    public string Description;

    [Export]
    public Texture2D Icon;

    [Export]
    public PackedScene AppScene;

    [Export]
    public bool IsSingleton = false;
}
