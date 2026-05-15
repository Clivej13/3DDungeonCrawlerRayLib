using System.Numerics;

namespace DungeonCrawler.Entities;

public abstract class Entity
{
    public Vector2 Position { get; protected set; }

    protected Entity(Vector2 position)
    {
        Position = position;
    }
}
