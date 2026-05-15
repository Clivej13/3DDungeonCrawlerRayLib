namespace DungeonCrawler.Core;

/// <summary>
/// State coordinator with menu history support for nested parent/child menu flows.
/// </summary>
public sealed class GameStateController
{
    private readonly Stack<GameState> _menuHistory = new();

    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
    }

    public void OpenMenu(GameState targetMenu)
    {
        _menuHistory.Push(CurrentState);
        CurrentState = targetMenu;
    }

    public void GoBack()
    {
        if (_menuHistory.Count > 0)
        {
            CurrentState = _menuHistory.Pop();
        }
    }

    public void ReturnToMainMenu()
    {
        _menuHistory.Clear();
        CurrentState = GameState.MainMenu;
    }
}
