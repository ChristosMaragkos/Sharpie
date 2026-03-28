using Godot;

namespace SharpieStudio.Desktop;

public partial class Clock : PanelContainer
{
    [Export]
    public Label DateLabel { get; private set; }

    [Export]
    public Label TimeLabel { get; private set; }

    public override void _Process(double delta)
    {
        var date = Time.GetDateDictFromSystem(); // YY/MM/DD/Weekday
        var time = Time.GetTimeDictFromSystem(); // HH/MM/SS

        DateLabel.Text = $"{date["day"]}/{date["month"]}/{date["year"]}";
        TimeLabel.Text =
            $"{time["hour"]}:{time["minute"]}:{FormatSeconds(time["second"].AsString())}";
    }

    public static string FormatSeconds(string seconds)
    {
        return int.Parse(seconds) < 10 ? $"0{seconds}" : seconds;
    }
}
