using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Player;

public sealed class PlayerController
{
    private readonly DungeonMap _map;

    public Vector2 Position { get; private set; }
    public float Angle { get; private set; }
    public float PitchOffset { get; private set; } = 0f;

    public float MoveSpeed { get; set; } = 170f;
    public float RotationSpeed { get; set; } = 2.4f;
    public float MouseSensitivityX { get; set; } = 0.0035f;
    public float MouseSensitivityY { get; set; } = 0.65f;

    public PlayerController(DungeonMap map)
    {
        _map = map;
        Position = map.PlayerSpawn;
        Angle = map.PlayerSpawnAngle;
    }

    public void Update(float deltaTime)
    {
        var mouseDelta = Raylib.GetMouseDelta();
        Angle += mouseDelta.X * MouseSensitivityX;
        Angle = MathF.IEEERemainder(Angle, MathF.Tau);

        // Doom-style fake pitch via horizon offset only, not true vertical rotation.
        PitchOffset -= mouseDelta.Y * MouseSensitivityY;
        PitchOffset = Math.Clamp(PitchOffset, -120f, 120f);

        float turnInput = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.Left)) turnInput -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.Right)) turnInput += 1f;
        Angle += turnInput * RotationSpeed * deltaTime;

        Vector2 forward = new(MathF.Cos(Angle), MathF.Sin(Angle));
        Vector2 right = new(-forward.Y, forward.X);

        float forwardAxis = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.W)) forwardAxis += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.S)) forwardAxis -= 1f;

        float strafeAxis = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.D)) strafeAxis += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.A)) strafeAxis -= 1f;

        Vector2 velocity = (forward * forwardAxis + right * strafeAxis);
        if (velocity.LengthSquared() > 1f)
        {
            velocity = Vector2.Normalize(velocity);
        }

        Vector2 desired = Position + velocity * MoveSpeed * deltaTime;
        TryMove(desired);
    }

    private void TryMove(Vector2 desired)
    {
        const float collisionRadius = 12f;

        // Axis-separated collision helps sliding along walls.
        float newX = desired.X;
        if (!HitsWall(newX, Position.Y, collisionRadius))
        {
            Position = new Vector2(newX, Position.Y);
        }

        float newY = desired.Y;
        if (!HitsWall(Position.X, newY, collisionRadius))
        {
            Position = new Vector2(Position.X, newY);
        }
    }

    private bool HitsWall(float x, float y, float radius)
    {
        return _map.IsWallAtWorld(x - radius, y - radius)
            || _map.IsWallAtWorld(x + radius, y - radius)
            || _map.IsWallAtWorld(x - radius, y + radius)
            || _map.IsWallAtWorld(x + radius, y + radius);
    }
}
