using DungeonCrawler.Core;
using DungeonCrawler.Input;
using Raylib_cs;

namespace DungeonCrawler.States;

public sealed class SettingsMenuScreen
{
    private readonly GameStateController _stateController;
    private readonly WindowSettings _windowSettings;
    private int _selectedIndex;
    private readonly string[] _entries = { "Resolution", "Fullscreen", "Back" };

    public SettingsMenuScreen(GameStateController stateController, WindowSettings windowSettings)
    {
        _stateController = stateController;
        _windowSettings = windowSettings;
    }

    public void Update(InputHandler input)
    {
        if (input.MoveUpPressed()) _selectedIndex = (_selectedIndex - 1 + _entries.Length) % _entries.Length;
        if (input.MoveDownPressed()) _selectedIndex = (_selectedIndex + 1) % _entries.Length;

        if (_selectedIndex == 0 && (input.MoveLeftPressed() || input.MoveRightPressed() || input.ConfirmPressed()))
        {
            _windowSettings.CycleResolution(input.MoveLeftPressed() ? -1 : 1);
        }

        if (_selectedIndex == 1 && input.ConfirmPressed()) _windowSettings.ToggleFullscreen();

        if ((_selectedIndex == 2 && input.ConfirmPressed()) || input.BackPressed()) _stateController.GoBack();
    }

    public void Draw()
    {
        DrawTitle("SETTINGS");
        var (w, h) = _windowSettings.CurrentResolution;
        DrawRow(0, $"Resolution: {w}x{h}", 210);
        DrawRow(1, $"Fullscreen: {(_windowSettings.IsFullscreen ? "On" : "Off")}", 264);
        DrawRow(2, "Back", 318);
        Raylib.DrawText("LEFT/RIGHT: Resolution   ENTER: Confirm   ESC: Back", 30, Raylib.GetScreenHeight() - 40, 20, Color.DarkGray);
    }

    private void DrawTitle(string title)
    {
        int sw = Raylib.GetScreenWidth();
        int tw = Raylib.MeasureText(title, 40);
        Raylib.DrawText(title, (sw - tw) / 2, 90, 40, Color.Gold);
    }

    private void DrawRow(int rowIndex, string text, int y)
    {
        int sw = Raylib.GetScreenWidth();
        bool selected = rowIndex == _selectedIndex;
        string label = selected ? $"> {text} <" : text;
        int tw = Raylib.MeasureText(label, 28);
        int x = (sw - tw) / 2;
        if (selected) Raylib.DrawRectangle(x - 18, y - 6, tw + 36, 40, new Color(45, 26, 17, 180));
        Raylib.DrawText(label, x, y, 28, selected ? Color.Orange : Color.RayWhite);
    }
}
