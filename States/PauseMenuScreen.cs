using DungeonCrawler.Core;
using DungeonCrawler.Input;
using DungeonCrawler.UI;
using Raylib_cs;

namespace DungeonCrawler.States;

public sealed class PauseMenuScreen : MenuBase
{
    private readonly GameStateController _stateController;

    public PauseMenuScreen(GameStateController stateController)
    {
        _stateController = stateController;
        SetOptions(new[]
        {
            new MenuOption("Resume", () => _stateController.ChangeState(GameState.Gameplay)),
            new MenuOption("Options", () => _stateController.OpenMenu(GameState.SettingsMenu)),
            new MenuOption("Controls", () => _stateController.OpenMenu(GameState.ControlsMenu)),
            new MenuOption("Exit To Main Menu", () => _stateController.ReturnToMainMenu()),
            new MenuOption("Exit Game", () => _stateController.ChangeState(GameState.Exiting))
        });
    }

    public override void Update(InputHandler input)
    {
        // Escape in pause toggles back to gameplay.
        base.Update(input);
        if (input.BackPressed())
        {
            _stateController.ChangeState(GameState.Gameplay);
        }
    }

    public void Draw()
    {
        Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), new Color(0, 0, 0, 170));
        DrawMenuTitle("PAUSED");
        DrawOptions(190);
    }
}
