using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Entities;

public enum EnemyCombatState
{
    Idle,
    Chasing,
    TelegraphingAttack,
    Attacking,
    Recovering,
    Staggered,
    Dead
}

public abstract class Enemy : Entity
{
    public float Health { get; protected set; }
    public float Radius { get; protected set; }
    public float MoveSpeed { get; protected set; }
    public bool IsAlive => Health > 0f;
    public float DistanceToPlayer { get; set; }
    public float HitFlashAmount => Math.Clamp(_hitFlashTimer / 0.15f, 0f, 1f);
    public EnemyCombatState CombatState { get; protected set; } = EnemyCombatState.Idle;
    public abstract Texture2D CurrentTexture { get; }

    private float _hitFlashTimer;

    protected Enemy(Vector2 position) : base(position) { }

    public virtual void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (!IsAlive) return;
        Health -= damage;
        _hitFlashTimer = 0.15f;
        if (!IsAlive) CombatState = EnemyCombatState.Dead;
    }

    public abstract void Update(float dt, Vector2 playerPos, DungeonMap map);
    public void SetPosition(Vector2 position) => Position = position;

    protected bool HitsWall(DungeonMap map, float x, float y)
    {
        return map.IsBlockedAtWorld(x - Radius, y - Radius)
            || map.IsBlockedAtWorld(x + Radius, y - Radius)
            || map.IsBlockedAtWorld(x - Radius, y + Radius)
            || map.IsBlockedAtWorld(x + Radius, y + Radius);
    }

    protected void TickTimers(float dt) => _hitFlashTimer = MathF.Max(0f, _hitFlashTimer - dt);
}
