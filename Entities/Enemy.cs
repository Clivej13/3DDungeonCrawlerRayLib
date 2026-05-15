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

    protected Enemy(Vector2 position, Texture2D texture) : base(position)
    {
        Texture = texture;
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive) return;
        Health -= damage;
    }

    public abstract void Update(float dt, Vector2 playerPos, DungeonMap map);

    protected bool HitsWall(DungeonMap map, float x, float y)
    {
        return map.IsWallAtWorld(x - Radius, y - Radius)
            || map.IsWallAtWorld(x + Radius, y - Radius)
            || map.IsWallAtWorld(x - Radius, y + Radius)
            || map.IsWallAtWorld(x + Radius, y + Radius);
    }
}
