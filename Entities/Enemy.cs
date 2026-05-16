using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Entities;

public abstract class Enemy : Entity
{
    public float Health { get; protected set; }
    public float Radius { get; protected set; }
    public float MoveSpeed { get; protected set; }
    public bool IsAlive => Health > 0f;
    public Texture2D Texture { get; }
    public float DistanceToPlayer { get; set; }
    public float HitFlashAmount => Math.Clamp(_hitFlashTimer / 0.15f, 0f, 1f);

    private float _hitFlashTimer;

    protected Enemy(Vector2 position, Texture2D texture) : base(position)
    {
        Texture = texture;
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;
        Health -= damage;
        _hitFlashTimer = 0.15f;

        Console.WriteLine($"[Combat] {GetType().Name} took {damage:0} damage. HP={MathF.Max(0f, Health):0}");
        if (!IsAlive)
        {
            Console.WriteLine($"[Combat] {GetType().Name} died.");
        }
    }

    public abstract void Update(float dt, Vector2 playerPos, DungeonMap map);

    public void SetPosition(Vector2 position)
    {
        Position = position;
    }

    protected bool HitsWall(DungeonMap map, float x, float y)
    {
        return map.IsWallAtWorld(x - Radius, y - Radius)
            || map.IsWallAtWorld(x + Radius, y - Radius)
            || map.IsWallAtWorld(x - Radius, y + Radius)
            || map.IsWallAtWorld(x + Radius, y + Radius);
    }

    protected void TickTimers(float dt)
    {
        _hitFlashTimer = MathF.Max(0f, _hitFlashTimer - dt);
    }
}
