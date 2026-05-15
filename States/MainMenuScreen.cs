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
        BuildOptions();
    }

    private void BuildOptions()
    {
        SetOptions(new[]
        {
            new MenuOption("Start Game", () => _stateController.ChangeState(GameState.Gameplay)),
            new MenuOption("Settings", () => _stateController.OpenSettings(GameState.MainMenu)),
            new MenuOption("Exit", () => _stateController.ChangeState(GameState.Exiting))
        });
    }

    public void Draw()
    {
        DrawMenuTitle("3D DUNGEON CRAWLER");
        DrawOptions(220);
        Raylib.DrawText("ENTER: Select   ESC: Quit", 30, Raylib.GetScreenHeight() - 40, 20, Color.DarkGray);
    }

    public override void Update(InputHandler input)
    {
        base.Update(input);

        if (input.BackPressed())
        {
            _stateController.ChangeState(GameState.Exiting);
        }
    }
}
