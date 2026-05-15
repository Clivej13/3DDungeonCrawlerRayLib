using DungeonCrawler.Input;
using Raylib_cs;

namespace DungeonCrawler.UI;

/// <summary>
/// Reusable scrollable menu list with keyboard and mouse wheel navigation.
/// Useful for controls, lore, inventory, and future rebinding lists.
/// </summary>
public abstract class ScrollableMenuBase
{
    protected readonly List<MenuOption> Options = new();
    protected int SelectedIndex;
    protected int TopVisibleIndex;

    protected int VisibleRows { get; init; } = 7;

    protected void SetOptions(IEnumerable<MenuOption> options)
    {
        Options.Clear();
        Options.AddRange(options);
        SelectedIndex = 0;
        TopVisibleIndex = 0;
    }

    public virtual void Update(InputHandler input)
    {
        if (Options.Count == 0) return;

        if (input.MoveUpPressed()) SelectedIndex = Math.Max(0, SelectedIndex - 1);
        if (input.MoveDownPressed()) SelectedIndex = Math.Min(Options.Count - 1, SelectedIndex + 1);

        var wheel = input.MouseWheelDelta();
        if (wheel > 0) SelectedIndex = Math.Max(0, SelectedIndex - 1);
        if (wheel < 0) SelectedIndex = Math.Min(Options.Count - 1, SelectedIndex + 1);

        KeepSelectionVisible();

        if (input.ConfirmPressed())
        {
            Options[SelectedIndex].Action.Invoke();
        }
    }

    protected void KeepSelectionVisible()
    {
        if (SelectedIndex < TopVisibleIndex) TopVisibleIndex = SelectedIndex;
        if (SelectedIndex >= TopVisibleIndex + VisibleRows) TopVisibleIndex = SelectedIndex - VisibleRows + 1;
    }

    protected void DrawMenuTitle(string title)
    {
        int sw = Raylib.GetScreenWidth();
        int w = Raylib.MeasureText(title, 40);
        Raylib.DrawText(title, (sw - w) / 2, 80, 40, Color.Gold);
    }

    protected void DrawScrollableOptions(int startY, int rowHeight = 48)
    {
        int sw = Raylib.GetScreenWidth();
        int end = Math.Min(TopVisibleIndex + VisibleRows, Options.Count);

        for (int i = TopVisibleIndex; i < end; i++)
        {
            var selected = i == SelectedIndex;
            var label = selected ? $"> {Options[i].Label} <" : Options[i].Label;
            var color = selected ? Color.Orange : Color.RayWhite;
            int tw = Raylib.MeasureText(label, 24);
            int x = (sw - tw) / 2;
            int y = startY + ((i - TopVisibleIndex) * rowHeight);

            if (selected)
            {
                Raylib.DrawRectangle(x - 16, y - 5, tw + 32, 35, new Color(45, 26, 17, 190));
            }

            Raylib.DrawText(label, x, y, 24, color);
        }

        if (TopVisibleIndex > 0)
            Raylib.DrawText("^", sw - 50, startY - 20, 20, Color.Gray);
        if (TopVisibleIndex + VisibleRows < Options.Count)
            Raylib.DrawText("v", sw - 50, startY + (VisibleRows * rowHeight), 20, Color.Gray);
    }
}
