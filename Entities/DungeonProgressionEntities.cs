using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Entities;

public enum KeyType
{
    Silver,
    Gold
}

public sealed class KeyItem : Entity
{
    public KeyType Type { get; }
    public Texture2D Texture { get; }
    public bool IsCollected { get; private set; }

    public KeyItem(Vector2 position, KeyType type, Texture2D texture) : base(position)
    {
        Type = type;
        Texture = texture;
    }

    public void Collect() => IsCollected = true;
}

public sealed class DoorEntity : Entity
{
    public KeyType Type { get; }
    public bool IsLocked { get; private set; }

    public DoorEntity(Vector2 position, KeyType type, bool isLocked) : base(position)
    {
        Type = type;
        IsLocked = isLocked;
    }

    public void Unlock() => IsLocked = false;
}
