using Godot;
using SharpieStudio.Abstractions;
using SharpieStudio.Desktop;

namespace SharpieStudio.Apps;

public partial class DesktopIcon : VBoxContainer, IAppIcon, IConfigurable<AppResource>
{
    public AppResource Data { get; set; }

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

    public int Id { get; set; }

    private Label _label;
    private TextureRect _icon;
    private bool _isDragging = false;
    private Vector2 _dragOffset;
    private Vector2I _currentGridCell;

    private StyleBoxFlat _selectedStyle;

    [Signal]
    public delegate void OpenRequestedEventHandler(AppResource data);

    // I don't know how well property accessors play with Godot's CallGroup, so I'll add another method
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
    }

    public void RequestOpenScene(AppResource data)
    {
        EmitSignalOpenRequested(data);
    }

    public override void _Ready()
    {
        GuiInput += OnGuiInput;
        CallDeferred(MethodName.InitialSnap);
    }

    private void InitialSnap()
    {
        _currentGridCell = DesktopManager.WorldToGrid(Position);
        DesktopManager.SetCellOccupied(_currentGridCell, true);
        Position = DesktopManager.GridToWorld(_currentGridCell);
    }

    public void SetInitialCell(Vector2I startCell)
    {
        _currentGridCell = startCell;
        DesktopManager.SetCellOccupied(_currentGridCell, true);
        Position = DesktopManager.GridToWorld(_currentGridCell);
    }

    public void Configure(AppResource data)
    {
        AddToGroup("DesktopIcons");

        _label = GetNode<Label>("Text");
        _icon = GetNode<TextureRect>("Icon");

        _selectedStyle = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0.5f),
            ContentMarginLeft = 2,
            ContentMarginRight = 2,
        };

        Data = data;
        _label.Text = Data.AppName;
        _icon.Texture = Data.Icon;
        TooltipText = Data.Description;
    }

    private void OnGuiInput(InputEvent @event)
    {
        AcceptEvent();
        if (@event is InputEventMouseButton mouse && mouse.ButtonIndex is MouseButton.Left)
        {
            if (mouse.Pressed)
            {
                this.UnfocusAppIcons();

                IsSelected = true;
                if (mouse.DoubleClick)
                {
                    GD.Print($"Launching: {_label.Text}");
                    RequestOpenScene(Data);
                    return;
                }

                _isDragging = true;
                _dragOffset = GetGlobalMousePosition() - GlobalPosition;

                DesktopManager.SetCellOccupied(_currentGridCell, false);
            }
            else
            {
                _isDragging = false;

                Vector2I targetCell = DesktopManager.WorldToGrid(Position);

                var desktopSize = GetParent<Control>().Size;
                int maxCol = Mathf.Max(
                    0,
                    Mathf.FloorToInt(desktopSize.X / DesktopManager.CellSize.X) - 1
                );
                int maxRow = Mathf.Max(
                    0,
                    Mathf.FloorToInt(desktopSize.Y / DesktopManager.CellSize.Y) - 1
                );

                targetCell.X = Mathf.Clamp(targetCell.X, 0, maxCol);
                targetCell.Y = Mathf.Clamp(targetCell.Y, 0, maxRow);

                if (DesktopManager.IsCellOccupied(targetCell))
                {
                    targetCell = _currentGridCell;
                }

                _currentGridCell = targetCell;
                DesktopManager.SetCellOccupied(_currentGridCell, true);

                Position = DesktopManager.GridToWorld(_currentGridCell);
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_isDragging)
        {
            Vector2 targetPos = GetGlobalMousePosition() - _dragOffset;
            GlobalPosition = targetPos;
        }
    }
}
