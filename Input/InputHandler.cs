using Raylib_cs;

namespace DungeonCrawler.Input;

/// <summary>
/// Centralized input queries so state/menu classes stay focused on behavior.
/// </summary>
public sealed class InputHandler
{
    public bool MoveUpPressed() => Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Up);
    public bool MoveDownPressed() => Raylib.IsKeyPressed(KeyboardKey.S) || Raylib.IsKeyPressed(KeyboardKey.Down);
    public bool MoveLeftPressed() => Raylib.IsKeyPressed(KeyboardKey.A) || Raylib.IsKeyPressed(KeyboardKey.Left);
    public bool MoveRightPressed() => Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsKeyPressed(KeyboardKey.Right);
    public bool ConfirmPressed() => Raylib.IsKeyPressed(KeyboardKey.Enter);
    public bool BackPressed() => Raylib.IsKeyPressed(KeyboardKey.Escape);
}
