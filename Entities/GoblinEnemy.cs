using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Entities;

public enum GoblinAttackType
{
    Light,
    Heavy
}

public sealed class GoblinEnemy : Enemy
{
    public const float AggroRange = 360f;
    public const float AttackRange = 52f;
    public const float FacingDotThreshold = 0.45f;

    public const float LightAttackWindup = 0.25f;
    public const float LightAttackActive = 0.15f;
    public const float LightAttackRecovery = 0.45f;
    public const float LightAttackDamage = 12f;
    public const float LightBlockStaggerDuration = 0.45f;

    public const float HeavyAttackWindup = 0.8f;
    public const float HeavyAttackActive = 0.2f;
    public const float HeavyAttackRecovery = 0.75f;
    public const float HeavyAttackDamage = 30f;
    public const float HeavyBlockStaggerDuration = 0.15f;

    public const float HitStaggerDuration = 0.3f;

    private readonly Texture2D _idleTexture;
    private readonly Texture2D _lightWindupTexture;
    private readonly Texture2D _heavyWindupTexture;
    private readonly Texture2D _strikeTexture;
    private readonly Texture2D _staggerTexture;
    private bool _useHeavyNext;

    public GoblinAttackType CurrentAttackType { get; private set; } = GoblinAttackType.Light;

    public override Texture2D CurrentTexture => CombatState switch
    {
        EnemyCombatState.TelegraphingAttack when CurrentAttackType == GoblinAttackType.Light => _lightWindupTexture,
        EnemyCombatState.TelegraphingAttack when CurrentAttackType == GoblinAttackType.Heavy => _heavyWindupTexture,
        EnemyCombatState.Attacking => _strikeTexture,
        EnemyCombatState.Staggered => _staggerTexture,
        _ => _idleTexture
    };

    public Vector2 FacingDirection { get; private set; } = Vector2.UnitX;
    public float StateTimer { get; private set; }
    public bool HasLineOfSightToPlayer { get; private set; }
    public bool CanDamagePlayerThisFrame { get; private set; }

    public float CurrentAttackDamage => CurrentAttackType == GoblinAttackType.Light ? LightAttackDamage : HeavyAttackDamage;
    public float CurrentBlockStaggerDuration => CurrentAttackType == GoblinAttackType.Light ? LightBlockStaggerDuration : HeavyBlockStaggerDuration;

    public GoblinEnemy(Vector2 position, Texture2D idleTexture, Texture2D lightWindupTexture, Texture2D heavyWindupTexture, Texture2D strikeTexture, Texture2D staggerTexture) : base(position)
    {
        _idleTexture = idleTexture;
        _lightWindupTexture = lightWindupTexture;
        _heavyWindupTexture = heavyWindupTexture;
        _strikeTexture = strikeTexture;
        _staggerTexture = staggerTexture;

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
                if (StateTimer <= 0f) EnterState(EnemyCombatState.Attacking, GetAttackActiveDuration(CurrentAttackType));
                break;
            case EnemyCombatState.Attacking:
                CanDamagePlayerThisFrame = true;
                if (StateTimer <= 0f) EnterState(EnemyCombatState.Recovering, GetAttackRecoveryDuration(CurrentAttackType));
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

    public void ApplyBlockStagger(float duration)
    {
        if (!IsAlive) return;
        EnterState(EnemyCombatState.Staggered, duration);
    }

    public override void TakeDamage(float damage, Vector2 hitDirection)
    {
        base.TakeDamage(damage, hitDirection);
        if (!IsAlive) return;
        Position -= hitDirection * 6f;
        EnterState(EnemyCombatState.Staggered, HitStaggerDuration);
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
            CurrentAttackType = SelectNextAttackType();
            EnterState(EnemyCombatState.TelegraphingAttack, GetAttackWindupDuration(CurrentAttackType));
            return;
        }

        EnterState(EnemyCombatState.Chasing, 0f);
        Vector2 dir = Vector2.Normalize(toPlayer);
        Vector2 desired = Position + dir * MoveSpeed * dt;
        if (!HitsWall(map, desired.X, Position.Y)) Position = new Vector2(desired.X, Position.Y);
        if (!HitsWall(map, Position.X, desired.Y)) Position = new Vector2(Position.X, desired.Y);
    }

    private GoblinAttackType SelectNextAttackType()
    {
        _useHeavyNext = !_useHeavyNext;
        return _useHeavyNext ? GoblinAttackType.Heavy : GoblinAttackType.Light;
    }

    private static float GetAttackWindupDuration(GoblinAttackType type) => type == GoblinAttackType.Light ? LightAttackWindup : HeavyAttackWindup;
    private static float GetAttackActiveDuration(GoblinAttackType type) => type == GoblinAttackType.Light ? LightAttackActive : HeavyAttackActive;
    private static float GetAttackRecoveryDuration(GoblinAttackType type) => type == GoblinAttackType.Light ? LightAttackRecovery : HeavyAttackRecovery;

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
