using DungeonCrawler.Core;
using DungeonCrawler.Input;
using DungeonCrawler.UI;
using Raylib_cs;

namespace DungeonCrawler.States;

public sealed class MainMenuScreen : MenuBase
{
    private readonly GameStateController _stateController;

    public MainMenuScreen(GameStateController stateController)
    {
        _stateController = stateController;
        SetOptions(new[]
        {
            new MenuOption("Start Game", () => _stateController.ChangeState(GameState.Gameplay)),
            new MenuOption("Settings", () => _stateController.OpenMenu(GameState.SettingsMenu)),
            new MenuOption("Controls", () => _stateController.OpenMenu(GameState.ControlsMenu)),
            new MenuOption("Exit", () => _stateController.ChangeState(GameState.Exiting))
        });
    }

    public override void Update(InputHandler input)
    {
        // Intentionally do not exit on ESC from main menu; only explicit Exit option should quit.
        base.Update(input);
    }

    public void Draw()
    {
        DrawMenuTitle("3D DUNGEON CRAWLER");
        DrawOptions(210);
        Raylib.DrawText("UP/DOWN or W/S: Navigate", 30, Raylib.GetScreenHeight() - 70, 20, Color.DarkGray);
        Raylib.DrawText("ENTER: Select", 30, Raylib.GetScreenHeight() - 40, 20, Color.DarkGray);
    }
}
