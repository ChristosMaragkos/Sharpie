using System.Linq;
using Godot;
using SharpieStudio.Abstractions;
using SharpieStudio.Desktop;

namespace SharpieStudio.Apps;

public partial class TaskbarIcon : VBoxContainer, ISelectable, IConfigurable<AppResource>
{
    public AppResource Data { get; set; }

    [Export]
    public TextureRect[] AppIcons { get; set; }

    [Export]
    public PanelContainer SelectedIndicator { get; set; }

    private WindowFrame BoundWindow { get; set; }

    public bool IsSelected
    {
        get;
        set
        {
            field = value;
            if (value)
            {
                SelectedIndicator.Visible = true;
                AppIcons.First().Visible = false;
            }
            else
            {
                SelectedIndicator.Visible = false;
                AppIcons.First().Visible = true;
            }
        }
    }

    public override void _Ready()
    {
        base._Ready();
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
            this.UnfocusAppIcons();
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    [Signal]
    public delegate void OpenRequestedEventHandler(AppResource data);

    public void RequestOpenScene(AppResource data)
    {
        EmitSignalOpenRequested(data);
    }

    public void Configure(AppResource data)
    {
        Data = data;
        foreach (var rect in AppIcons)
            rect.Texture = data.Icon;

        TooltipText = $"\t{data.FileName}\n{data.Description}";
    }
}
