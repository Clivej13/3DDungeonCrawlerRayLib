namespace DungeonCrawler.Core;

/// <summary>
/// Central state transition coordinator.
/// Tracks where Settings was opened from so it can return correctly.
/// </summary>
public sealed class GameStateController
{
    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    private GameState _settingsReturnState = GameState.MainMenu;

    public void ChangeState(GameState newState) => CurrentState = newState;

    public void OpenSettings(GameState fromState)
    {
        _settingsReturnState = fromState;
        CurrentState = GameState.Settings;
    }

    public void ReturnFromSettings()
    {
        CurrentState = _settingsReturnState;
    }
}
