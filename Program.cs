using DungeonCrawler.Core;
using DungeonCrawler.Input;
using DungeonCrawler.States;
using Raylib_cs;

class Program
{
    static void Main()
    {
        var options = GameOptions.LoadOrCreateDefaults();
        void SaveOptions() => GameOptions.Save(options);

        var settings = new WindowSettings(options, SaveOptions);
        var (w, h) = settings.CurrentResolution;
        Raylib.InitWindow(w, h, "3DDungeonCrawlerRayLib");

        if (options.IsFullscreen)
        {
            Raylib.ToggleFullscreen();
        }

        // Disable Raylib default ESC-to-close behavior
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(60);

        var input = new InputHandler();
        var stateController = new GameStateController();

        var mainMenu = new MainMenuScreen(stateController);
        var settingsMenu = new SettingsMenuScreen(stateController, settings, options, SaveOptions);
        var controlsMenu = new ControlsMenuScreen(stateController);
        using var gameplay = new GameplayScreen(stateController, options);
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
            [GameState.MainMenu] = mainMenu.Draw,
            [GameState.SettingsMenu] = settingsMenu.Draw,
            [GameState.ControlsMenu] = controlsMenu.Draw,
            [GameState.Gameplay] = gameplay.Draw,
            [GameState.PauseMenu] = () => { gameplay.Draw(); pauseMenu.Draw(); }
        };

        bool running = true;
        while (running)
        {
            if (Raylib.WindowShouldClose())
            {
                running = false;
                continue;
            }

            float dt = Raylib.GetFrameTime();

            if (stateController.CurrentState == GameState.Exiting)
            {
                running = false;
            }
            else if (updates.TryGetValue(stateController.CurrentState, out var update))
            {
                update(dt);
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(8, 8, 12, 255));
            if (draws.TryGetValue(stateController.CurrentState, out var draw))
            {
                draw();
            }
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
