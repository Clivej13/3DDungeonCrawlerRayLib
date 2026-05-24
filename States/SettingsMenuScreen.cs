using DungeonCrawler.Core;
using DungeonCrawler.Input;
using Raylib_cs;

namespace DungeonCrawler.States;

public sealed class SettingsMenuScreen
{
    private readonly GameStateController _stateController;
    private readonly WindowSettings _windowSettings;
    private readonly GameOptions _options;
    private readonly Action _saveOptions;
    private int _selectedIndex;
    private readonly string[] _entries =
    {
        "Resolution",
        "Fullscreen",
        "Debug Mode",
        "Combat Debug",
        "Disable Health",
        "Disable Collision",
        "Disable Key Requirements",
        "Back"
    };

    public SettingsMenuScreen(GameStateController stateController, WindowSettings windowSettings, GameOptions options, Action saveOptions)
    {
        _stateController = stateController;
        _windowSettings = windowSettings;
        _options = options;
        _saveOptions = saveOptions;
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
        if (_selectedIndex == 2 && input.ConfirmPressed()) ToggleOption(() => _options.DebugModeEnabled = !_options.DebugModeEnabled);
        if (_selectedIndex == 3 && input.ConfirmPressed()) ToggleOption(() => _options.CombatDebugOverlayEnabled = !_options.CombatDebugOverlayEnabled);
        if (_selectedIndex == 4 && input.ConfirmPressed()) ToggleOption(() => _options.DisableHealth = !_options.DisableHealth);
        if (_selectedIndex == 5 && input.ConfirmPressed()) ToggleOption(() => _options.DisableCollision = !_options.DisableCollision);
        if (_selectedIndex == 6 && input.ConfirmPressed()) ToggleOption(() => _options.DisableKeyRequirements = !_options.DisableKeyRequirements);

        if ((_selectedIndex == 7 && input.ConfirmPressed()) || input.BackPressed()) _stateController.GoBack();
    }

    public void Draw()
    {
        DrawTitle("SETTINGS");
        var (w, h) = _windowSettings.CurrentResolution;
        DrawRow(0, $"Resolution: {w}x{h}", 170);
        DrawRow(1, $"Fullscreen: {(_windowSettings.IsFullscreen ? "ON" : "OFF")}", 214);
        DrawRow(2, $"Debug Mode: {(_options.DebugModeEnabled ? "ON" : "OFF")}", 258);
        DrawRow(3, $"Combat Debug: {(_options.CombatDebugOverlayEnabled ? "ON" : "OFF")}", 302);
        DrawRow(4, $"Disable Health: {(_options.DisableHealth ? "ON" : "OFF")}", 346);
        DrawRow(5, $"Disable Collision: {(_options.DisableCollision ? "ON" : "OFF")}", 390);
        DrawRow(6, $"Disable Key Requirements: {(_options.DisableKeyRequirements ? "ON" : "OFF")}", 434);
        DrawRow(7, "Back", 478);
        Raylib.DrawText("LEFT/RIGHT: Resolution   ENTER: Toggle/Confirm   ESC: Back", 30, Raylib.GetScreenHeight() - 40, 20, Color.DarkGray);
    }

    private void ToggleOption(Action toggle)
    {
        toggle();
        _saveOptions();
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
