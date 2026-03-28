using Godot;

namespace SharpieStudio.Desktop;

public partial class DesktopBackground : CanvasLayer
{
    [Export]
    private TextureRect Background { get; set; }

    public override void _Ready()
    {
        base._Ready();
        Background.GuiInput += (input) =>
        {
            if (input is InputEventMouseButton m && m.Pressed)
                this.UnfocusAppIcons();
        };
    }
}
