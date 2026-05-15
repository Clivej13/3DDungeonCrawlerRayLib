using DungeonCrawler.Core;
using DungeonCrawler.Input;
using DungeonCrawler.UI;
using Raylib_cs;

namespace DungeonCrawler.States;

public sealed class ControlsMenuScreen : ScrollableMenuBase
{
    private readonly GameStateController _stateController;

    public ControlsMenuScreen(GameStateController stateController)
    {
        _stateController = stateController;

        SetOptions(new[]
        {
            new MenuOption("Move Forward = W", () => { }),
            new MenuOption("Move Backward = S", () => { }),
            new MenuOption("Strafe Left = A", () => { }),
            new MenuOption("Strafe Right = D", () => { }),
            new MenuOption("Attack = Left Mouse Button", () => { }),
            new MenuOption("Pause/Menu = Escape", () => { }),
            new MenuOption("Interact = E", () => { }),
            new MenuOption("Sprint = Left Shift", () => { }),
            new MenuOption("Back", () => _stateController.GoBack())
        });
    }

    public override void Update(InputHandler input)
    {
        base.Update(input);
        if (input.BackPressed()) _stateController.GoBack();
    }

    public void Draw()
    {
        DrawMenuTitle("CONTROLS");
        DrawScrollableOptions(190, 44);
        Raylib.DrawText("Mouse Wheel or W/S: Scroll   ENTER: Select   ESC: Back", 30, Raylib.GetScreenHeight() - 40, 20, Color.DarkGray);
    }
}
