using Godot;

namespace SharpieStudio.Apps;

public interface ISelectable
{
    void RequestOpenScene(AppResource data);

    [Export]
    public AppResource Data { get; set; }
}
