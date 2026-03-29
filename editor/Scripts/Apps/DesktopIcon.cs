using Godot;
using SharpieStudio.Desktop;

namespace SharpieStudio.Apps;

public partial class DesktopIcon : VBoxContainer
{
    [Export]
    public AppResource Data { get; set; }

    public bool IsSelected
    {
        get;
        set
        {
            field = value;
            if (value)
            {
                _label.AddThemeStyleboxOverride("normal", _selectedStyle);
            }
            else
            {
                _label.RemoveThemeStyleboxOverride("normal");
            }
        }
    }

    // I don't know how well property accessors play with Godot's CallGroup, so I'll add another method
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
    }

    private Label _label;
    private TextureRect _icon;
    private bool _isDragging = false;
    private Vector2 _dragOffset;

    private StyleBoxFlat _selectedStyle;

    public override void _Ready()
    {
        AddToGroup("DesktopIcons");

        _label = GetNode<Label>("Text");
        _icon = GetNode<TextureRect>("Icon");

        _label.Text = Data.FileName;
        _icon.Texture = Data.Icon;

        _selectedStyle = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0.5f),
            ContentMarginLeft = 2,
            ContentMarginRight = 2,
        };

        GuiInput += OnGuiInput;
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouse && mouse.ButtonIndex is MouseButton.Left)
        {
            if (mouse.Pressed)
            {
                AcceptEvent();
                this.UnfocusAppIcons();

                IsSelected = true;
                if (mouse.DoubleClick)
                    GD.Print($"Launching: {_label.Text}");

                _isDragging = true;
                _dragOffset = GetGlobalMousePosition() - GlobalPosition;
            }
            else
            {
                _isDragging = false;
                // TODO: Grid snap
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_isDragging)
        {
            GlobalPosition = GetGlobalMousePosition() - _dragOffset;
        }
    }
}
