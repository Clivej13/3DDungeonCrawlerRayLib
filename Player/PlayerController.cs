using DungeonCrawler.Core;
using DungeonCrawler.Entities;
using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Player;

public sealed class PlayerController
{
    private readonly DungeonMap _map;
    private readonly GameOptions _options;

    public Vector2 Position { get; private set; }
    public float Angle { get; private set; }
    public float PitchOffset { get; private set; } = 0f;
    public Vector2 MoveInputDirection { get; private set; }

    public float MoveSpeed { get; set; } = 170f;
    public float RotationSpeed { get; set; } = 2.4f;
    public float MouseSensitivityX { get; set; } = 0.0035f;
    public float MouseSensitivityY { get; set; } = 0.65f;
    public float Health { get; private set; } = 100f;
    public bool IsAlive => Health > 0f;
    public bool IsInvulnerable => _invulnerabilityTimer > 0f;
    public HashSet<string> CollectedKeyIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsMoving { get; private set; }

    private float _invulnerabilityTimer;
    private float _damageFlashTimer;
    private float _targetPitchOffset;

    public float DamageFlashAmount => Math.Clamp(_damageFlashTimer / 0.12f, 0f, 1f);

    public PlayerController(DungeonMap map, GameOptions options)
    {
        _map = map;
        _options = options;
        Position = map.PlayerSpawn;
        Angle = map.PlayerSpawnAngle;
    }

    public void Update(float deltaTime, IEnumerable<Enemy> enemies)
    {
        if (!IsAlive)
        {
            MoveInputDirection = Vector2.Zero;
            IsMoving = false;
            return;
        }

        _invulnerabilityTimer = MathF.Max(0f, _invulnerabilityTimer - deltaTime);
        _damageFlashTimer = MathF.Max(0f, _damageFlashTimer - deltaTime);

        var mouseDelta = Raylib.GetMouseDelta();
        Angle += mouseDelta.X * MouseSensitivityX;
        Angle = MathF.IEEERemainder(Angle, MathF.Tau);

        _targetPitchOffset -= mouseDelta.Y * MouseSensitivityY;
        _targetPitchOffset = Math.Clamp(_targetPitchOffset, -260f, 260f);
        float interpolation = 1f - MathF.Exp(-14f * deltaTime);
        PitchOffset += (_targetPitchOffset - PitchOffset) * interpolation;

        float turnInput = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.Left)) turnInput -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.Right)) turnInput += 1f;
        Angle += turnInput * RotationSpeed * deltaTime;

        Vector2 velocity = ReadMoveInput();
        MoveInputDirection = velocity;
        IsMoving = velocity.LengthSquared() > 0.0001f;
        Vector2 desired = Position + velocity * MoveSpeed * deltaTime;
        TryMoveTo(desired, _map, enemies);
    }

    public void TryMoveTo(Vector2 desired, DungeonMap map, IEnumerable<Enemy> enemies)
    {
        if (_options.DebugModeEnabled && _options.DisableCollision)
        {
            Position = desired;
            return;
        }

        const float collisionRadius = 12f;
        float newX = desired.X;
        if (!HitsWall(map, newX, Position.Y, collisionRadius) && !HitsEnemy(new Vector2(newX, Position.Y), enemies, collisionRadius))
        {
            Position = new Vector2(newX, Position.Y);
        }

        float newY = desired.Y;
        if (!HitsWall(map, Position.X, newY, collisionRadius) && !HitsEnemy(new Vector2(Position.X, newY), enemies, collisionRadius))
        {
            Position = new Vector2(Position.X, newY);
        }
    }

    public bool TryTakeDamage(float damage)
    {
        if (_options.DebugModeEnabled && _options.DisableHealth) return false;
        if (!IsAlive || IsInvulnerable) return false;

        Health = MathF.Max(0f, Health - damage);
        _invulnerabilityTimer = 0.5f;
        _damageFlashTimer = 0.12f;
        return true;
    }

    public void AddKey(string keyId)
    {
        if (!string.IsNullOrWhiteSpace(keyId)) CollectedKeyIds.Add(keyId);
    }

    public bool HasKey(string? keyId)
        => !string.IsNullOrWhiteSpace(keyId) && CollectedKeyIds.Contains(keyId);

    private Vector2 ReadMoveInput()
    {
        Vector2 forward = new(MathF.Cos(Angle), MathF.Sin(Angle));
        Vector2 right = new(-forward.Y, forward.X);

        float forwardAxis = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.W)) forwardAxis += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.S)) forwardAxis -= 1f;

        float strafeAxis = 0f;
        if (Raylib.IsKeyDown(KeyboardKey.D)) strafeAxis += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.A)) strafeAxis -= 1f;

        Vector2 velocity = (forward * forwardAxis + right * strafeAxis);
        if (velocity.LengthSquared() > 1f) velocity = Vector2.Normalize(velocity);
        return velocity;
    }

    private static bool HitsEnemy(Vector2 position, IEnumerable<Enemy> enemies, float radius)
        => enemies.Any(e => e.IsAlive && Vector2.DistanceSquared(position, e.Position) < MathF.Pow(radius + e.Radius, 2f));

    private static bool HitsWall(DungeonMap map, float x, float y, float radius)
    {
        return map.IsBlockedAtWorld(x - radius, y - radius)
            || map.IsBlockedAtWorld(x + radius, y - radius)
            || map.IsBlockedAtWorld(x - radius, y + radius)
            || map.IsBlockedAtWorld(x + radius, y + radius);
    }
}
