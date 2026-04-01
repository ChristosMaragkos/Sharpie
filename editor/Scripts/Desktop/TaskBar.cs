using System;
using Godot;

namespace SharpieStudio.Desktop;

public partial class TaskBar : PanelContainer
{
    private static TaskBar Instance { get; set; }

    [Export]
    public Button StartButton { get; set; }

    [Export]
    public PackedScene StartMenuScene { get; set; }

    public static bool IsStartMenuOpen { get; set; }

    public override void _Ready()
    {
        Instance ??= this;
        StartButton.Pressed += OnStartButtonPressed;
    }

    private void OnStartButtonPressed()
    {
        if (!IsStartMenuOpen)
            GetTree().Root.AddChild(StartMenuScene.Instantiate<Control>());
        else
            GetTree().CallGroup("StartMenu", Node.MethodName.QueueFree);

        IsStartMenuOpen = !IsStartMenuOpen;
    }

    public static void CloseStartMenu()
    {
        Instance.GetTree().CallGroup("StartMenu", Node.MethodName.QueueFree);
        IsStartMenuOpen = false;
    }
}
