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
        // Disable Raylib default ESC-to-close behavior
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(60);

        var input = new InputHandler();
        var stateController = new GameStateController();

        var mainMenu = new MainMenuScreen(stateController);
        var settingsMenu = new SettingsMenuScreen(stateController, settings);
        var controlsMenu = new ControlsMenuScreen(stateController);
        GameplayScreen gameplay = new GameplayScreen(stateController);
        var pauseMenu = new PauseMenuScreen(stateController);

        void StartNewGameplaySession()
        {
            gameplay.Dispose();
            gameplay = new GameplayScreen(stateController);
        }

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
            [GameState.Gameplay] = () => gameplay.Draw(),
            [GameState.PauseMenu] = () => { gameplay.Draw(); pauseMenu.Draw(); }
        };

        GameState previousState = stateController.CurrentState;

        bool running = true;
        while (running)
        {
            // WindowShouldClose is for OS-level close requests (X button / platform close event).
            // It should terminate app immediately and must never be reused as pause/menu input.
            if (Raylib.WindowShouldClose())
            {
                running = false;
                continue;
            }

            float dt = Raylib.GetFrameTime();

            if (previousState == GameState.MainMenu && stateController.CurrentState == GameState.Gameplay)
            {
                StartNewGameplaySession();
            }
            previousState = stateController.CurrentState;

            if (stateController.CurrentState == GameState.Exiting)
            {
                running = false;
            }
            else if (updates.TryGetValue(stateController.CurrentState, out var update))
            {
                // Exactly one state update per frame ensures Escape is processed once.
                update(dt);

                if (stateController.CurrentState == GameState.Gameplay)
                {
                    if (gameplay.RequestNewGame)
                    {
                        StartNewGameplaySession();
                        stateController.ChangeState(GameState.Gameplay);
                    }
                    else if (gameplay.RequestMainMenu)
                    {
                        stateController.ReturnToMainMenu();
                    }
                }
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
        gameplay.Dispose();
    }
}
