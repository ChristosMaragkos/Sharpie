using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SharpieStudio.Apps;

namespace SharpieStudio.Desktop;

public partial class DesktopManager : Node
{
    [Export]
    public Control DesktopArea { get; set; }

    [Export]
    public Control QuickLaunchArea { get; set; }

    private const int DesktopHeight = 1080 - 60;
    private const int DesktopWidth = 1920 - 64;
    private static readonly PackedScene WindowScene = GD.Load<PackedScene>(
        "res://Scenes/window_frame.tscn"
    );

    private static readonly PackedScene DesktopIconScene = GD.Load<PackedScene>(
        "res://Scenes/desktop_icon.tscn"
    );

    private static readonly PackedScene TaskbarIconScene = GD.Load<PackedScene>(
        "res://Scenes/taskbar_icon.tscn"
    );

    private static readonly Dictionary<string, Process> OpenWindows = [];

    public static readonly Vector2 CellSize = new(64, 100);

    private static readonly HashSet<Vector2I> OccupiedCells = [];

    public static Vector2I WorldToGrid(Vector2 worldPosition)
    {
        return new Vector2I(
            Mathf.RoundToInt(worldPosition.X / CellSize.X),
            Mathf.RoundToInt(worldPosition.Y / CellSize.Y)
        );
    }

    public static Vector2 GridToWorld(Vector2I gridPosition)
    {
        return new Vector2(gridPosition.X * CellSize.X, gridPosition.Y * CellSize.Y);
    }

    public static bool IsCellOccupied(Vector2I cell) => OccupiedCells.Contains(cell);

    public static void SetCellOccupied(Vector2I cell, bool occupied)
    {
        if (occupied)
            OccupiedCells.Add(cell);
        else
            OccupiedCells.Remove(cell);
    }

    public static Vector2I GetNextAvailableCell(Vector2 desktopSize)
    {
        int maxCols = Mathf.Max(1, Mathf.FloorToInt(desktopSize.X / CellSize.X));
        int maxRows = Mathf.Max(1, Mathf.FloorToInt(desktopSize.Y / CellSize.Y));

        for (int x = 0; x < maxCols; x++)
        {
            for (int y = 0; y < maxRows; y++)
            {
                Vector2I cell = new(x, y);
                if (!IsCellOccupied(cell))
                {
                    return cell;
                }
            }
        }
        return Vector2I.Zero;
    }

    public override void _Ready()
    {
        ChildEnteredTree += OnWindowCreated;

        foreach (var win in GetChildren().OfType<WindowFrame>())
        {
            win.OnCloseRequested += OnWindowCloseRequested;
        }
        LoadApps();
    }

    private void LoadApps()
    {
        using var dirAccess = DirAccess.Open("res://Resources/Apps");

        foreach (
            var resourceFile in dirAccess
                .GetFiles()
                .Where(fp => fp.EndsWith(".tres") || fp.EndsWith(".res"))
                .Select(fp => dirAccess.GetCurrentDir() + "/" + fp)
        )
        {
            var appData = GD.Load<AppResource>(resourceFile);
            AddAppIcon(appData);
        }

        dirAccess.Dispose();
    }

    private void OnWindowCreated(Node node)
    {
        if (node is WindowFrame win)
        {
            win.OnCloseRequested += OnWindowCloseRequested;
            win.FocusEntered += this.UnfocusAppIcons;
        }
    }

    public void TryOpenWindow(AppResource data)
    {
        if (OpenWindows.TryGetValue(data.AppName, out var open))
        {
            open.ActiveWinwow?.BringToFront();
            open.TaskbarIcon.IsSelected = true;
            return;
        }

        AddWindow(data);
    }

    public void AddWindow(AppResource data)
    {
        this.UnfocusAppIcons();
        var window = WindowScene.Instantiate<WindowFrame>();

        window.Configure(data);
        window.OnCloseRequested += OnWindowCloseRequested;
        DesktopArea.AddChild(window);

        Process process = new()
        {
            DesktopIcon = DesktopArea
                .GetChildren()
                .OfType<DesktopIcon>()
                .First(icon => icon.Data.AppScene == data.AppScene),
            TaskbarIcon = QuickLaunchArea
                .GetChildren()
                .OfType<TaskbarIcon>()
                .First(icon => icon.Data.AppScene == data.AppScene),
        };
        process.TaskbarIcon.BoundWindow = window;
        process.TaskbarIcon.IsSelected = true;
        window.OnFocusRequested += () => FocusTaskbarIcon(process);

        OpenWindows[data.AppName] = process;
    }

    private static void FocusTaskbarIcon(Process process)
    {
        process.TaskbarIcon.IsSelected = true;
    }

    public void AddAppIcon(AppResource appResource)
    {
        var appIcon = DesktopIconScene.Instantiate<DesktopIcon>();
        appIcon.Configure(appResource);
        DesktopArea.AddChild(appIcon);
        appIcon.OpenRequested += TryOpenWindow;

        Vector2I startingCell = GetNextAvailableCell(DesktopArea.Size);
        appIcon.SetInitialCell(startingCell);

        var taskbarIcon = TaskbarIconScene.Instantiate<TaskbarIcon>();
        taskbarIcon.Configure(appResource);
        QuickLaunchArea.AddChild(taskbarIcon);
        taskbarIcon.OpenRequested += TryOpenWindow;
    }

    private void OnWindowCloseRequested(WindowFrame win)
    {
        OpenWindows.Remove(win.TitleLabel.Text, out Process proc);
        win.OnFocusRequested -= () => FocusTaskbarIcon(proc);
        win.QueueFree();
        this.UnfocusAppIcons();
    }
}
