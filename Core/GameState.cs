namespace DungeonCrawler.Core;

/// <summary>
/// High-level game states. Keep this enum small and route to sub-menus where needed.
/// </summary>
public enum GameState
{
    MainMenu,
    Settings,
    Gameplay,
    PauseMenu,
    Exiting
}
