using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Entities;

public sealed class GoblinEnemy : Enemy
{
    public const float AggroRange = 360f;
    public const float AttackRange = 52f;
    public const float AttackDamage = 12f;
    public const float TelegraphDuration = 0.6f;
    public const float AttackDuration = 0.2f;
    public const float RecoveryDuration = 0.65f;
    public const float StaggerDuration = 0.3f;
    public const float FacingDotThreshold = 0.45f;

    private readonly Texture2D _idleTexture;
    private readonly Texture2D _windupTexture;
    private readonly Texture2D _attackTexture;

    public override Texture2D CurrentTexture => CombatState switch
    {
        EnemyCombatState.TelegraphingAttack => _windupTexture,
        EnemyCombatState.Attacking => _attackTexture,
        EnemyCombatState.Staggered => _attackTexture,
        _ => _idleTexture
    };

    public Vector2 FacingDirection { get; private set; } = Vector2.UnitX;
    public float StateTimer { get; private set; }
    public bool HasLineOfSightToPlayer { get; private set; }
    public bool CanDamagePlayerThisFrame { get; private set; }

    public GoblinEnemy(Vector2 position, Texture2D idleTexture, Texture2D windupTexture, Texture2D attackTexture) : base(position)
    {
        _idleTexture = idleTexture;
        _windupTexture = windupTexture;
        _attackTexture = attackTexture;

        Health = 40f;
        Radius = 12f;
        MoveSpeed = 85f;
    }

    public override void Update(float dt, Vector2 playerPos, DungeonMap map)
    {
        if (!IsAlive) { CombatState = EnemyCombatState.Dead; return; }
        TickTimers(dt);
        StateTimer = MathF.Max(0f, StateTimer - dt);
        CanDamagePlayerThisFrame = false;

        Vector2 toPlayer = playerPos - Position;
        float dist = toPlayer.Length();
        if (dist > 0.001f) FacingDirection = Vector2.Normalize(toPlayer);
        HasLineOfSightToPlayer = HasLineOfSight(map, playerPos);

        switch (CombatState)
        {
            case EnemyCombatState.Idle:
            case EnemyCombatState.Chasing:
                UpdateChasing(dt, toPlayer, dist, map);
                break;
            case EnemyCombatState.TelegraphingAttack:
                if (StateTimer <= 0f) EnterState(EnemyCombatState.Attacking, AttackDuration);
                break;
            case EnemyCombatState.Attacking:
                CanDamagePlayerThisFrame = true;
                if (StateTimer <= 0f) EnterState(EnemyCombatState.Recovering, RecoveryDuration);
                break;
            case EnemyCombatState.Recovering:
                if (StateTimer <= 0f) EnterState(EnemyCombatState.Chasing, 0f);
                break;
            case EnemyCombatState.Staggered:
                if (StateTimer <= 0f) EnterState(EnemyCombatState.Chasing, 0f);
                break;
        }
    }

    public bool IsAttackValid(Vector2 playerPos, DungeonMap map)
    {
        Vector2 toPlayer = playerPos - Position;
        float dist = toPlayer.Length();
        if (dist <= 0.001f || dist > AttackRange) return false;
        if (!HasLineOfSight(map, playerPos)) return false;
        Vector2 dir = toPlayer / dist;
        return Vector2.Dot(FacingDirection, dir) >= FacingDotThreshold;
    }

    public override void TakeDamage(float damage, Vector2 hitDirection)
    {
        base.TakeDamage(damage, hitDirection);
        if (!IsAlive) return;
        Position -= hitDirection * 6f;
        EnterState(EnemyCombatState.Staggered, StaggerDuration);
    }

    private void UpdateChasing(float dt, Vector2 toPlayer, float dist, DungeonMap map)
    {
        if (dist > AggroRange || !HasLineOfSightToPlayer)
        {
            EnterState(EnemyCombatState.Idle, 0f);
            return;
        }

        if (dist <= AttackRange)
        {
            EnterState(EnemyCombatState.TelegraphingAttack, TelegraphDuration);
            return;
        }

        EnterState(EnemyCombatState.Chasing, 0f);
        Vector2 dir = Vector2.Normalize(toPlayer);
        Vector2 desired = Position + dir * MoveSpeed * dt;
        if (!HitsWall(map, desired.X, Position.Y)) Position = new Vector2(desired.X, Position.Y);
        if (!HitsWall(map, Position.X, desired.Y)) Position = new Vector2(Position.X, desired.Y);
    }

    private void EnterState(EnemyCombatState next, float duration)
    {
        if (CombatState == next && next != EnemyCombatState.Attacking) return;
        CombatState = next;
        StateTimer = duration;
    }

    private bool HasLineOfSight(DungeonMap map, Vector2 playerPos)
    {
        Vector2 delta = playerPos - Position;
        float dist = delta.Length();
        if (dist <= 0.001f) return true;
        Vector2 dir = delta / dist;
        const float step = 8f;
        for (float t = 0f; t <= dist; t += step)
        {
            Vector2 sample = Position + dir * t;
            if (map.IsBlockedAtWorld(sample.X, sample.Y)) return false;
        }
        return true;
    }
}
