using Godot;
using SharpieStudio.OS;

namespace SharpieStudio.Apps;

public partial class FileExplorer : VBoxContainer
{
    [Export]
    public Texture2D FolderIcon { get; set; }

    [Export]
    public Texture2D CFileIcon { get; set; }

    [Export]
    public Texture2D AsmFileIcon { get; set; }

    [Export]
    public Texture2D TextFileIcon { get; set; }

    private Button _upButton;
    private LineEdit _addressBar;
    private ItemList _fileView;

    public string CurrentPath { get; set; } = "user://Desktop";

    public override void _Ready()
    {
        _upButton = GetNode<Button>("Toolbar/UpButton");
        _addressBar = GetNode<LineEdit>("Toolbar/AddressBar");
        _fileView = GetNode<ItemList>("FileView");

        _upButton.Pressed += NavigateUp;
        _addressBar.TextSubmitted += NavigateTo;
        _fileView.ItemActivated += OnItemDoubleClicked;

        LoadDirectory(CurrentPath);
    }

    private void LoadDirectory(string path)
    {
        if (!DirAccess.DirExistsAbsolute(path))
            return;

        CurrentPath = path;
        _addressBar.Text = path.Replace("user:/", "~");
        _fileView.Clear();

        using var dir = DirAccess.Open(path);

        foreach (string dirName in dir.GetDirectories())
        {
            int index = _fileView.AddItem(dirName, FolderIcon);
            // We store custom metadata so we know it's a folder when clicked
            _fileView.SetItemMetadata(index, "dir");
        }

        foreach (string fileName in dir.GetFiles())
        {
            Texture2D icon = TextFileIcon;
            if (fileName.EndsWith(".c"))
                icon = CFileIcon;
            else if (fileName.EndsWith(".asm"))
                icon = AsmFileIcon;

            int index = _fileView.AddItem(fileName, icon);
            _fileView.SetItemMetadata(index, "file");
        }
    }

    private void NavigateUp()
    {
        if (CurrentPath == "user://Desktop")
            return;

        string newPath = CurrentPath.TrimEnd('/');
        int lastSlash = newPath.LastIndexOf('/');
        if (lastSlash > 0)
        {
            LoadDirectory(newPath.Substring(0, lastSlash));
        }
        else
        {
            LoadDirectory("user://Desktop");
        }
    }

    private void NavigateTo(string newPath)
    {
        if (DirAccess.DirExistsAbsolute(newPath))
            LoadDirectory(newPath);
        else
            _addressBar.Text = CurrentPath; // Revert if invalid
    }

    private void OnItemDoubleClicked(long index)
    {
        string itemName = _fileView.GetItemText((int)index);
        string itemType = _fileView.GetItemMetadata((int)index).AsString();

        if (itemType == "dir")
        {
            string nextPath = CurrentPath.EndsWith('/')
                ? CurrentPath + itemName
                : CurrentPath + "/" + itemName;
            LoadDirectory(nextPath);
        }
        else if (itemType == "file")
        {
            Texture2D icon = _fileView.GetItemIcon((int)index);
            string absolutePath = CurrentPath.EndsWith('/')
                ? CurrentPath + itemName
                : CurrentPath + "/" + itemName;

            var fileResource = new AppResource
            {
                AppName = itemName,
                Icon = icon,
                // AppScene = NotepadScene // Soon
                Description = absolutePath,
            };

            SystemEvents.RequestAppLaunch(fileResource);
        }
    }
}
