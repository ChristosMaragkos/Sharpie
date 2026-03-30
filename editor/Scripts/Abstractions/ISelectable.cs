using Godot;
using SharpieStudio.Apps;

namespace SharpieStudio.Abstractions;

public interface ISelectable
{
    void RequestOpenScene(AppResource data);

    [Export]
    public AppResource Data { get; set; }
}
