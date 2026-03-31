using Godot;
using SharpieStudio.Abstractions;

namespace SharpieStudio.Apps;

public partial class TaskbarIcon : VBoxContainer, IAppIcon, IConfigurable<AppResource>
{
    public AppResource Data { get; set; }

    [Export]
    public TextureRect UnselectedIcon { get; set; }

    [Export]
    private TextureRect SelectedIcon { get; set; }

    [Export]
    public PanelContainer SelectedIndicator { get; set; }

    public WindowFrame? BoundWindow { get; set; }

    public bool IsSelected
    {
        get;
        set
        {
            field = value;
            if (value)
            {
                SelectedIndicator.Visible = true;
                UnselectedIcon.Visible = false;
            }
            else
            {
                SelectedIndicator.Visible = false;
                UnselectedIcon.Visible = true;
            }
        }
    }

    public int Id { get; set; }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
    }

    [Signal]
    public delegate void OpenRequestedEventHandler(AppResource data);

    public override void _Ready()
    {
        base._Ready();
        AddToGroup("DesktopIcons");
        GuiInput += OnGuiInput;
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (
            @event is InputEventMouseButton mouse
            && mouse.ButtonIndex is MouseButton.Left
            && mouse.Pressed
        )
        {
            AcceptEvent();
            RequestOpenScene(Data);
            IsSelected = true;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public void RequestOpenScene(AppResource data)
    {
        EmitSignalOpenRequested(data);
    }

    public void Configure(AppResource data)
    {
        Data = data;
        UnselectedIcon.Texture = data.Icon;
        SelectedIcon.Texture = UnselectedIcon.Texture;
        SelectedIcon.SelfModulate = Colors.LightGray;
        TooltipText = $"\t{data.AppName}\n{data.Description}";
    }
}
