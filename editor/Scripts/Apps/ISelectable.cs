using Godot;

namespace SharpieStudio.Apps;

public interface ISelectable
{
    void RequestOpenScene(PackedScene sceneToOpen, string appName);

    [Export]
    public AppResource Data { get; set; }
}
