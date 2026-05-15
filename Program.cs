using DungeonCrawler.Core;
using DungeonCrawler.Input;
using DungeonCrawler.States;
using Raylib_cs;

class Program
{
    static void Main()
    {
        var settings = new WindowSettings();
        var (initialWidth, initialHeight) = settings.CurrentResolution;

        Raylib.InitWindow(initialWidth, initialHeight, "3DDungeonCrawlerRayLib");
        Raylib.SetTargetFPS(60);

        var input = new InputHandler();
        var stateController = new GameStateController();

        var mainMenu = new MainMenuScreen(stateController);
        var settingsMenu = new SettingsMenuScreen(stateController, settings);
        var gameplay = new GameplayScreen(stateController);
        var pauseMenu = new PauseMenuScreen(stateController);

        bool isRunning = true;

        while (isRunning && !Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();

            switch (stateController.CurrentState)
            {
                case GameState.MainMenu:
                    mainMenu.Update(input);
                    break;
                case GameState.Settings:
                    settingsMenu.Update(input);
                    break;
                case GameState.Gameplay:
                    gameplay.Update(input, deltaTime);
                    break;
                case GameState.PauseMenu:
                    pauseMenu.Update(input);
                    break;
                case GameState.Exiting:
                    isRunning = false;
                    break;
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(8, 8, 12, 255));

            switch (stateController.CurrentState)
            {
                case GameState.MainMenu:
                    mainMenu.Draw();
                    break;
                case GameState.Settings:
                    settingsMenu.Draw();
                    break;
                case GameState.Gameplay:
                    gameplay.Draw();
                    break;
                case GameState.PauseMenu:
                    gameplay.Draw();
                    pauseMenu.Draw();
                    break;
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
