using Godot;
using SharpieStudio.Apps;

namespace SharpieStudio.Abstractions;

public interface IAppIcon
{
    int Id { get; set; }

    [Export]
    public AppResource Data { get; set; }

    void RequestOpenScene(AppResource data);
    void SetSelected(bool selected);
}
