using DungeonCrawler.Input;
using Raylib_cs;

namespace DungeonCrawler.UI;

/// <summary>
/// Reusable vertical menu with keyboard navigation and retro-styled rendering.
/// </summary>
public abstract class MenuBase
{
    private readonly List<MenuOption> _options = new();
    private int _selectedIndex;

    protected void SetOptions(IEnumerable<MenuOption> options)
    {
        _options.Clear();
        _options.AddRange(options);
        _selectedIndex = 0;
    }

    public virtual void Update(InputHandler input)
    {
        if (_options.Count == 0)
        {
            return;
        }

        if (input.MoveUpPressed())
        {
            _selectedIndex = (_selectedIndex - 1 + _options.Count) % _options.Count;
        }

        if (input.MoveDownPressed())
        {
            _selectedIndex = (_selectedIndex + 1) % _options.Count;
        }

        if (input.ConfirmPressed())
        {
            _options[_selectedIndex].Action.Invoke();
        }
    }

    protected void DrawMenuTitle(string title)
    {
        int screenWidth = Raylib.GetScreenWidth();
        int titleWidth = Raylib.MeasureText(title, 40);
        Raylib.DrawText(title, (screenWidth - titleWidth) / 2, 90, 40, Color.Gold);
    }

    protected void DrawOptions(int startY)
    {
        int screenWidth = Raylib.GetScreenWidth();
        const int itemHeight = 54;

        for (int i = 0; i < _options.Count; i++)
        {
            var isSelected = i == _selectedIndex;
            var label = isSelected ? $"> {_options[i].Label} <" : _options[i].Label;
            var color = isSelected ? Color.Orange : Color.RayWhite;
            int textWidth = Raylib.MeasureText(label, 28);
            int x = (screenWidth - textWidth) / 2;
            int y = startY + (i * itemHeight);

            if (isSelected)
            {
                Raylib.DrawRectangle(x - 18, y - 6, textWidth + 36, 40, new Color(45, 26, 17, 180));
            }

            Raylib.DrawText(label, x, y, 28, color);
        }
    }
}
