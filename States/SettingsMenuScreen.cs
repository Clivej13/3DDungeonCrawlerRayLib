using DungeonCrawler.Core;
using DungeonCrawler.Input;
using DungeonCrawler.UI;
using Raylib_cs;

namespace DungeonCrawler.States;

public sealed class SettingsMenuScreen : MenuBase
{
    private readonly GameStateController _stateController;
    private readonly WindowSettings _windowSettings;

    private readonly string[] _entries =
    {
        "Resolution",
        "Fullscreen",
        "Back"
    };

    private int _selectedIndex;

    public SettingsMenuScreen(GameStateController stateController, WindowSettings windowSettings)
    {
        _stateController = stateController;
        _windowSettings = windowSettings;
    }

    public override void Update(InputHandler input)
    {
        if (input.MoveUpPressed())
        {
            _selectedIndex = (_selectedIndex - 1 + _entries.Length) % _entries.Length;
        }

        if (input.MoveDownPressed())
        {
            _selectedIndex = (_selectedIndex + 1) % _entries.Length;
        }

        if (_selectedIndex == 0 && (input.MoveLeftPressed() || input.MoveRightPressed() || input.ConfirmPressed()))
        {
            _windowSettings.CycleResolution(input.MoveLeftPressed() ? -1 : 1);
        }

        if (_selectedIndex == 1 && input.ConfirmPressed())
        {
            _windowSettings.ToggleFullscreen();
        }

        if ((_selectedIndex == 2 && input.ConfirmPressed()) || input.BackPressed())
        {
            _stateController.ReturnFromSettings();
        }
    }

    public void Draw()
    {
        DrawMenuTitle("SETTINGS");

        int centerX = Raylib.GetScreenWidth() / 2;
        int startY = 220;
        const int step = 54;

        var (w, h) = _windowSettings.CurrentResolution;
        string resolutionValue = $"{w}x{h}";
        string fullscreenValue = _windowSettings.IsFullscreen ? "On" : "Off";

        DrawRow(0, $"Resolution: {resolutionValue}", centerX, startY, step);
        DrawRow(1, $"Fullscreen: {fullscreenValue}", centerX, startY, step);
        DrawRow(2, "Back", centerX, startY, step);

        Raylib.DrawText("LEFT/RIGHT: Change Resolution", 30, Raylib.GetScreenHeight() - 70, 20, Color.DarkGray);
        Raylib.DrawText("ENTER: Confirm   ESC: Back", 30, Raylib.GetScreenHeight() - 40, 20, Color.DarkGray);
    }

    private void DrawRow(int index, string value, int centerX, int startY, int step)
    {
        bool selected = index == _selectedIndex;
        string label = selected ? $"> {value} <" : value;
        Color color = selected ? Color.Orange : Color.RayWhite;
        int textWidth = Raylib.MeasureText(label, 28);
        int y = startY + (index * step);

        if (selected)
        {
            Raylib.DrawRectangle(centerX - (textWidth / 2) - 18, y - 6, textWidth + 36, 40, new Color(45, 26, 17, 180));
        }

        Raylib.DrawText(label, centerX - (textWidth / 2), y, 28, color);
    }
}
