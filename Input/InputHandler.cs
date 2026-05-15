using Raylib_cs;

namespace DungeonCrawler.Input;

/// <summary>
/// Centralized input queries for state-aware behavior.
/// Escape is intentionally exposed as BackPressed so each state handles it exactly once.
/// </summary>
public sealed class InputHandler
{
    public bool MoveUpPressed() => Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Up);
    public bool MoveDownPressed() => Raylib.IsKeyPressed(KeyboardKey.S) || Raylib.IsKeyPressed(KeyboardKey.Down);
    public bool MoveLeftPressed() => Raylib.IsKeyPressed(KeyboardKey.A) || Raylib.IsKeyPressed(KeyboardKey.Left);
    public bool MoveRightPressed() => Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsKeyPressed(KeyboardKey.Right);
    public bool ConfirmPressed() => Raylib.IsKeyPressed(KeyboardKey.Enter);
    public bool BackPressed() => Raylib.IsKeyPressed(KeyboardKey.Escape);
    public float MouseWheelDelta() => Raylib.GetMouseWheelMove();
}
