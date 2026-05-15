namespace DungeonCrawler.UI;

public sealed class MenuOption
{
    public MenuOption(string label, System.Action action)
    {
        Label = label;
        Action = action;
    }

    public string Label { get; }
    public System.Action Action { get; }
}
