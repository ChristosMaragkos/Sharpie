using System;
using Godot;
using SharpieStudio.Desktop;

namespace SharpieStudio.Apps;

public partial class WindowFrame : PanelContainer
{
    [Export]
    public MarginContainer AppArea { get; set; }

    [Export]
    public HBoxContainer TitleBarArea { get; set; }

    [Export]
    public Label TitleLabel { get; set; }

    #region Buttons
    [Export]
    public TextureButton CloseButton { get; set; }

    [Export]
    public TextureButton MaximizeButton { get; set; }

    [Export]
    public TextureButton MinimizeButton { get; set; }
    #endregion

    #region Button Events
    public event Action<WindowFrame> OnCloseRequested;

    public event Action<WindowFrame> OnMaximizeRequested;

    public event Action<WindowFrame> OnMinimizeRequested;
    #endregion

    private bool _isDragging = false;
    private Vector2 _dragOffset;

    public override void _Ready()
    {
        CloseButton.Pressed += () => OnCloseRequested?.Invoke(this);
        TitleBarArea.GuiInput += OnTitleBarGuiInput;
        GuiInput += OnWindowGuiInput;
    }

    private void OnTitleBarGuiInput(InputEvent @event)
    {
        if (
            @event is InputEventMouseButton mouseEvent
            && mouseEvent.ButtonIndex is MouseButton.Left
        )
        {
            if (mouseEvent.Pressed)
            {
                this.UnfocusAppIcons();
                _isDragging = true;
                _dragOffset = GetGlobalMousePosition() - GlobalPosition;
                MoveToFront();
                AcceptEvent();
            }
            else
            {
                _isDragging = false;
            }
        }
    }

    private void OnWindowGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            MoveToFront();
        }
    }

    public override void _Process(double delta)
    {
        if (_isDragging)
        {
            var parent = GetParentOrNull<Control>();
            if (parent != null)
            {
                Vector2 targetPos = GetGlobalMousePosition() - _dragOffset;

                targetPos.X = Mathf.Clamp(targetPos.X, 0, parent.Size.X - Size.X);
                targetPos.Y = Mathf.Clamp(targetPos.Y, 0, parent.Size.Y - Size.Y);

                GlobalPosition = targetPos;
            }
        }
    }

    public void Configure(AppResource data)
    {
        TitleLabel.Text = data.FileName;

        var app = data.AppScene.Instantiate();
        AppArea.AddChild(app);
    }
}
