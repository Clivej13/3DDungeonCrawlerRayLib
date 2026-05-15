using DungeonCrawler.Core;
using DungeonCrawler.Input;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.States;

/// <summary>
/// Placeholder first-person gameplay state. Replace with world/camera systems later.
/// </summary>
public sealed class GameplayScreen
{
    private readonly GameStateController _stateController;
    private Vector2 _playerPosition = new(0, 0);

    public GameplayScreen(GameStateController stateController)
    {
        _stateController = stateController;
    }

    public void Update(InputHandler input, float deltaTime)
    {
        const float speed = 200f;

        if (Raylib.IsKeyDown(KeyboardKey.W)) _playerPosition.Y -= speed * deltaTime;
        if (Raylib.IsKeyDown(KeyboardKey.S)) _playerPosition.Y += speed * deltaTime;
        if (Raylib.IsKeyDown(KeyboardKey.A)) _playerPosition.X -= speed * deltaTime;
        if (Raylib.IsKeyDown(KeyboardKey.D)) _playerPosition.X += speed * deltaTime;

        if (input.BackPressed())
        {
            _stateController.ChangeState(GameState.PauseMenu);
        }
    }

    public void Draw()
    {
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();

        Raylib.DrawRectangle(0, 0, sw, sh, new Color(10, 10, 16, 255));
        Raylib.DrawRectangle(0, sh / 2, sw, sh / 2, new Color(24, 16, 12, 255));

        Raylib.DrawText("DUNGEON DEPTHS", 30, 20, 30, Color.Gold);
        Raylib.DrawText("WASD: Move   ESC: Pause", 30, 56, 20, Color.LightGray);
        Raylib.DrawText($"Player Pos: {_playerPosition.X:0}, {_playerPosition.Y:0}", 30, 86, 20, Color.Gray);

        // Retro crosshair
        Raylib.DrawLine(sw / 2 - 10, sh / 2, sw / 2 + 10, sh / 2, Color.RayWhite);
        Raylib.DrawLine(sw / 2, sh / 2 - 10, sw / 2, sh / 2 + 10, Color.RayWhite);
    }
}
