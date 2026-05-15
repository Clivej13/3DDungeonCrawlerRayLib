using DungeonCrawler.Core;
using DungeonCrawler.Input;
using DungeonCrawler.States;
using Raylib_cs;

class Program
{
    static void Main()
    {
        var settings = new WindowSettings();
        var (w, h) = settings.CurrentResolution;
        Raylib.InitWindow(w, h, "3DDungeonCrawlerRayLib");
        Raylib.SetTargetFPS(60);

        var input = new InputHandler();
        var stateController = new GameStateController();

        var mainMenu = new MainMenuScreen(stateController);
        var settingsMenu = new SettingsMenuScreen(stateController, settings);
        var controlsMenu = new ControlsMenuScreen(stateController);
        var gameplay = new GameplayScreen(stateController);
        var pauseMenu = new PauseMenuScreen(stateController);

        var updates = new Dictionary<GameState, Action<float>>
        {
            [GameState.MainMenu] = _ => mainMenu.Update(input),
            [GameState.SettingsMenu] = _ => settingsMenu.Update(input),
            [GameState.ControlsMenu] = _ => controlsMenu.Update(input),
            [GameState.Gameplay] = dt => gameplay.Update(input, dt),
            [GameState.PauseMenu] = _ => pauseMenu.Update(input)
        };

        var draws = new Dictionary<GameState, Action>
        {
            [GameState.MainMenu] = () => mainMenu.Draw(),
            [GameState.SettingsMenu] = () => settingsMenu.Draw(),
            [GameState.ControlsMenu] = () => controlsMenu.Draw(),
            [GameState.Gameplay] = () => gameplay.Draw(),
            [GameState.PauseMenu] = () => { gameplay.Draw(); pauseMenu.Draw(); }
        };

        bool running = true;
        while (running)
        {
            float dt = Raylib.GetFrameTime();

            // Don't let close requests bypass state behavior.
            // During gameplay, treat it like pause. Else ignore and keep menu-driven exits.
            if (Raylib.WindowShouldClose())
            {
                if (stateController.CurrentState == GameState.Gameplay)
                    stateController.ChangeState(GameState.PauseMenu);
            }

            if (stateController.CurrentState == GameState.Exiting)
            {
                running = false;
            }
            else
            {
                updates[stateController.CurrentState](dt);
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(8, 8, 12, 255));
            if (draws.TryGetValue(stateController.CurrentState, out var draw)) draw();
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
