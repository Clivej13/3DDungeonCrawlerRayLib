using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Entities;

public sealed class GoblinEnemy : Enemy
{
    private const float AggroRange = 360f;
    public bool IsChasing { get; private set; }

    public float Damage { get; } = 12f;
    public float AttackCooldown { get; } = 0.85f;
    public float AttackRange { get; } = 46f;

    private float _attackCooldownTimer;

    public GoblinEnemy(Vector2 position, Texture2D texture) : base(position, texture)
    {
        Health = 40f;
        Radius = 12f;
        MoveSpeed = 85f;
    }

    public override void Update(float dt, Vector2 playerPos, DungeonMap map)
    {
        if (!IsAlive) return;
        TickTimers(dt);
        _attackCooldownTimer = MathF.Max(0f, _attackCooldownTimer - dt);
        IsChasing = false;

        Vector2 toPlayer = playerPos - Position;
        float distSq = toPlayer.LengthSquared();
        if (distSq > AggroRange * AggroRange || distSq <= AttackRange * AttackRange) return;

        Vector2 dir = Vector2.Normalize(toPlayer);
        Vector2 desired = Position + dir * MoveSpeed * dt;
        IsChasing = true;

        if (!HitsWall(map, desired.X, Position.Y)) Position = new Vector2(desired.X, Position.Y);
        if (!HitsWall(map, Position.X, desired.Y)) Position = new Vector2(Position.X, desired.Y);
    }

    public bool CanAttackPlayer(Vector2 playerPos)
    {
        if (!IsAlive || _attackCooldownTimer > 0f) return false;
        return Vector2.DistanceSquared(playerPos, Position) <= AttackRange * AttackRange;
    }

    public float ConsumeAttackDamage()
    {
        _attackCooldownTimer = AttackCooldown;
        return Damage;
    }
}
