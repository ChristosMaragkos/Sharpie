using System;
using Godot;

namespace SharpieStudio;

public partial class DesktopIcon : VBoxContainer
{
    [Export]
    public string FileName = "New File";

    [Export]
    public Texture2D IconTexture;

    private bool IsSelected
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

    private Label _label;
    private TextureRect _icon;
    private bool _isDragging = false;
    private Vector2 _dragOffset;

    private StyleBoxFlat _selectedStyle;

    public override void _Ready()
    {
        _label = GetNode<Label>("Text");
        _icon = GetNode<TextureRect>("Icon");

        _label.Text = FileName;
        _icon.Texture = IconTexture;

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
                IsSelected = true;
                if (mouse.DoubleClick)
                    GD.Print($"Launching: {FileName}");

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
