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
    [ExportGroup("Buttons")]
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

    private bool _isResizing = false;
    private Vector2 _resizeStartMousePos;
    private Vector2 _resizeStartWindowSize;
    private Vector2 _resizeAxis;

    private bool _isMaximized = false;
    private Vector2 _restorePosition;
    private Vector2 _restoreSize;

    private static readonly Vector2 MinWindowSize = new(200, 150);
    private const float BorderThickness = 8f;

    public override void _Ready()
    {
        CloseButton.Pressed += () => OnCloseRequested?.Invoke(this);
        MaximizeButton.Pressed += ToggleMaximize;
        TitleBarArea.GuiInput += OnTitleBarGuiInput;
        GuiInput += OnWindowGuiInput;
    }

    private Vector2 GetResizeAxis(Vector2 localMousePos)
    {
        bool onRightEdge = localMousePos.X >= Size.X - BorderThickness;
        bool onBottomEdge = localMousePos.Y >= Size.Y - BorderThickness;

        if (onRightEdge && onBottomEdge)
            return new Vector2(1, 1);
        if (onRightEdge)
            return new Vector2(1, 0);
        if (onBottomEdge)
            return new Vector2(0, 1);

        return Vector2.Zero;
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
        if (@event is InputEventMouseMotion motion && !_isResizing && !_isDragging)
        {
            Vector2 axis = GetResizeAxis(motion.Position);

            if (axis == new Vector2(1, 1))
                MouseDefaultCursorShape = CursorShape.Fdiagsize;
            else if (axis == new Vector2(1, 0))
                MouseDefaultCursorShape = CursorShape.Hsize;
            else if (axis == new Vector2(0, 1))
                MouseDefaultCursorShape = CursorShape.Vsize;
            else
                MouseDefaultCursorShape = CursorShape.Arrow;
        }
        else if (
            @event is InputEventMouseButton mouseBtn
            && mouseBtn.ButtonIndex == MouseButton.Left
        )
        {
            if (mouseBtn.Pressed)
            {
                Vector2 axis = GetResizeAxis(mouseBtn.Position);

                if (axis != Vector2.Zero)
                {
                    this.UnfocusAppIcons();
                    _isResizing = true;
                    _resizeAxis = axis;
                    _resizeStartMousePos = GetGlobalMousePosition();
                    _resizeStartWindowSize = Size;
                    MoveToFront();
                    AcceptEvent();
                }
                else
                {
                    // just a normal click on the window body
                    MoveToFront();
                }
            }
            else
            {
                _isResizing = false;
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_isMaximized)
            return;

        if (_isDragging)
        {
            var parent = GetParentOrNull<Control>();
            if (parent != null)
            {
                Vector2 targetPos = GetGlobalMousePosition() - _dragOffset;

                float maxX = MathF.Max(0, parent.Size.X - Size.X);
                float maxY = MathF.Max(0, parent.Size.Y - Size.Y);

                targetPos.X = Mathf.Clamp(targetPos.X, 0, maxX);
                targetPos.Y = Mathf.Clamp(targetPos.Y, 0, maxY);

                GlobalPosition = targetPos;
            }
        }
        else if (_isResizing)
        {
            Vector2 mouseDelta = GetGlobalMousePosition() - _resizeStartMousePos;

            mouseDelta.X *= _resizeAxis.X;
            mouseDelta.Y *= _resizeAxis.Y;

            Vector2 newSize = _resizeStartWindowSize + mouseDelta;

            ApplyResize(newSize);
        }
    }

    public void Configure(AppResource data)
    {
        TitleLabel.Text = data.FileName;

        var app = data.AppScene.Instantiate();
        AppArea.AddChild(app);
    }

    private void ApplyResize(Vector2 targetSize)
    {
        var parent = GetParentOrNull<Control>();
        if (parent is null)
            return;

        targetSize.X = MathF.Max(targetSize.X, MinWindowSize.X);
        targetSize.Y = MathF.Max(targetSize.Y, MinWindowSize.Y);

        float maxAllowedWidth = parent.Size.X - GlobalPosition.X;
        float maxAllowedHeight = parent.Size.Y - GlobalPosition.Y;

        targetSize.X = MathF.Min(targetSize.X, maxAllowedWidth);
        targetSize.Y = MathF.Min(targetSize.Y, maxAllowedHeight);

        Size = targetSize;
    }

    private void ToggleMaximize()
    {
        var parent = GetParentOrNull<Control>();
        if (parent is null)
            return;

        if (_isMaximized)
        {
            GlobalPosition = _restorePosition;
            Size = _restoreSize;
            _isMaximized = false;
        }
        else
        {
            _restorePosition = GlobalPosition;
            _restoreSize = Size;

            Position = Vector2.Zero;
            Size = parent.Size;

            _isMaximized = true;
            MoveToFront();
        }
    }
}
