using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using FileAccess = Godot.FileAccess;

namespace SharpieStudio.Apps;

// TODO: Flags for ls, show files and folders in desktop
public partial class TerminalApp : PanelContainer, IApp
{
    [Export]
    public RichTextLabel OutputDisplay { get; set; }

    [Export]
    public LineEdit InputLine { get; set; }

    [Export]
    public Label PromptLabel { get; set; }

    private DirAccess _fs;

    private string CurrentDir => _fs.GetCurrentDir();

    public override void _Ready()
    {
        _fs = DirAccess.Open("user://");

        InputLine.TextSubmitted += OnTextSubmitted;
        InputLine.GrabFocus();

        PrintLine("SharpieOS v1.0.0 (tty1)");
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        string displayPath = _fs.GetCurrentDir().Replace("user://", "~");
        PromptLabel.Text = $"root@sharpie:{displayPath}$ ";
    }

    private void PrintLine(string text)
    {
        OutputDisplay.AppendText(text + '\n');
    }

    private void OnTextSubmitted(string newText)
    {
        PrintLine($"{PromptLabel.Text}{newText}");
        InputLine.Clear();
        ProcessCommand(newText.Trim());
        InputLine.GrabFocus();
    }

    private void ProcessCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        string[] rawArgs = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string cmd = rawArgs[0];

        HashSet<char> flags = rawArgs
            .Where(s => s.StartsWith('-'))
            .SelectMany(s => s.TrimStart('-'))
            .ToHashSet();

        string[] args = rawArgs
            .Where(s => !s.StartsWith('-'))
            .Skip(cmd == rawArgs[0] ? 1 : 0)
            .ToArray();

        switch (cmd)
        {
            case "pwd":
                PrintLine(CurrentDir.Replace("user:/", "~")); // yes, only one slash
                break;

            case "ls":
                ListDirectory(flags);
                break;

            case "cd":
                ChangeDirectory(rawArgs.Length > 1 ? rawArgs[1] : "user://");
                break;

            case "mkdir":
                if (rawArgs.Length > 1)
                    MakeDirectory(rawArgs[1]);
                else
                    PrintLine("mkdir: missing operand");
                break;

            case "touch":
                if (rawArgs.Length > 1)
                    CreateFile(rawArgs[1]);
                else
                    PrintLine("touch: missing operand");
                break;

            case "clear":
                OutputDisplay.Clear();
                break;

            case "exit":
                ((IApp)this).GetEnclosingWindow().RequestClose();
                break;

            case "cat":
                CatFile(args);
                break;

            case "rm":
                RemoveItem(args, flags);
                break;

            default:
                PrintLine($"{cmd}: command not found");
                break;
        }
    }

    private void ListDirectory(HashSet<char> flags)
    {
        string[] dirs = _fs.GetDirectories();
        string[] files = _fs.GetFiles();

        foreach (var dir in dirs)
        {
            PrintLine($"[color=lightblue]{dir}/[/color]");
        }

        foreach (var file in files)
        {
            string color = "white";

            if (file.EndsWith(".c"))
                color = "cyan";
            else if (file.EndsWith(".asm"))
                color = "red";

            PrintLine($"[color={color}]{file}[/color]");
        }
    }

    private void ChangeDirectory(string targetPath)
    {
        if (targetPath.StartsWith('~'))
        {
            targetPath = targetPath.Replace("~", "user://");
        }

        Error err = _fs.ChangeDir(targetPath);

        if (err is not Error.Ok)
        {
            PrintLine($"cd: {targetPath}: No such file directory. Error code: {err}");
        }
    }

    private void MakeDirectory(string dirName)
    {
        if (_fs.DirExists(dirName))
        {
            PrintLine($"mkdir: cannot create directory '{dirName}': Already exists");
            return;
        }

        Error err = _fs.MakeDir(dirName);
        if (err is not Error.Ok)
        {
            PrintLine(
                $"mkdir: cannot create directory '{dirName}': Permission denied. Error code: {err}"
            );
        }
    }

    private void CreateFile(string fileName)
    {
        string absolutePath = _fs.GetCurrentDir() + '/' + fileName;

        if (!FileAccess.FileExists(absolutePath))
        {
            using var file = FileAccess.Open(absolutePath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                PrintLine(
                    $"touch: cannot touch '{fileName}': Permission denied. Error code: {file.GetError()}"
                );
            }
        }
    }

    private void CatFile(string[] args)
    {
        if (args.Length == 0)
        {
            PrintLine("cat: missing file operand");
            return;
        }

        string filePath = _fs.GetCurrentDir() + '/' + args[0];

        if (!FileAccess.FileExists(filePath))
        {
            PrintLine($"cat: {args[0]}: No such file");
        }

        PrintLine(FileAccess.GetFileAsString(filePath));
    }

    private void RemoveItem(string[] args, HashSet<char> flags)
    {
        if (args.Length == 0)
        {
            PrintLine("rm: missing operand");
            return;
        }

        string target = args[0];
        bool isRecurseive = flags.Contains('f');
        bool force = flags.Contains('f');

        if (_fs.DirExists(target))
        {
            if (!isRecurseive)
            {
                PrintLine($"rm: cannot remove '{target}': Is a directory");
                return;
            }

            Error err = RemoveRecursive(_fs.GetCurrentDir() + '/' + target);
            if (err is not Error.Ok && !force)
                PrintLine($"rm: failed to remove '{target}'. Error code: {err}");
        }
        else if (_fs.FileExists(target))
        {
            Error err = _fs.Remove(target);
            if (err is not Error.Ok && !force)
                PrintLine($"rm: failed to remove '{target}'. Error code: {err}");
        }
        else if (!force)
        {
            PrintLine($"rm: cannot remove '{target}': No such file or directory");
        }
    }

    private static Error RemoveRecursive(string absolutePath)
    {
        using var dir = DirAccess.Open(absolutePath);
        if (dir is null)
            return Error.Failed;

        foreach (string file in dir.GetFiles())
            dir.Remove(file);

        foreach (string subdir in dir.GetDirectories())
            RemoveRecursive(absolutePath + '/' + subdir);

        return DirAccess.RemoveAbsolute(absolutePath);
    }
}
