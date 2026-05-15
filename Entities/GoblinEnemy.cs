using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Entities;

public sealed class GoblinEnemy : Enemy
{
    private const float AggroRange = 360f;
    private const float StopRange = 44f;

    public GoblinEnemy(Vector2 position, Texture2D texture) : base(position, texture)
    {
        Health = 40f;
        Radius = 12f;
        MoveSpeed = 85f;
    }

    public override void Update(float dt, Vector2 playerPos, DungeonMap map)
    {
        if (!IsAlive) return;

        Vector2 toPlayer = playerPos - Position;
        float distSq = toPlayer.LengthSquared();
        if (distSq > AggroRange * AggroRange || distSq <= StopRange * StopRange) return;

        Vector2 dir = Vector2.Normalize(toPlayer);
        Vector2 desired = Position + dir * MoveSpeed * dt;

        if (!HitsWall(map, desired.X, Position.Y)) Position = new Vector2(desired.X, Position.Y);
        if (!HitsWall(map, Position.X, desired.Y)) Position = new Vector2(Position.X, desired.Y);
    }
}
