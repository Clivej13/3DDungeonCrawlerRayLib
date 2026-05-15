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
            new MenuOption("Options", () => _stateController.OpenSettings(GameState.PauseMenu)),
            new MenuOption("Exit To Main Menu", () => _stateController.ChangeState(GameState.MainMenu))
        });
    }

    public void Draw()
    {
        Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), new Color(0, 0, 0, 170));
        DrawMenuTitle("PAUSED");
        DrawOptions(220);
    }

    public override void Update(InputHandler input)
    {
        base.Update(input);

        if (input.BackPressed())
        {
            _stateController.ChangeState(GameState.Gameplay);
        }
    }
}
