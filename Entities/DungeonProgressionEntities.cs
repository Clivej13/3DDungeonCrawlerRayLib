using DungeonCrawler.World;
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
    public DoorState State { get; private set; }
    public bool IsLocked => State == DoorState.Locked;
    public bool BlocksMovement => State is DoorState.Closed or DoorState.Locked or DoorState.Closing;
    public bool BlocksRaycast => BlocksMovement;
    public float TimeSinceOpened { get; private set; }

    public DoorEntity(Vector2 position, KeyType type, bool isLocked) : base(position)
    {
        Type = type;
        State = isLocked ? DoorState.Locked : DoorState.Closed;
    }

    public void Unlock() => State = DoorState.Closed;

    public void StartOpening()
    {
        State = DoorState.Opening;
        State = DoorState.Open;
        TimeSinceOpened = 0f;
    }

    public void StartClosing()
    {
        State = DoorState.Closing;
        State = DoorState.Closed;
    }

    public void Tick(float dt)
    {
        if (State == DoorState.Open) TimeSinceOpened += dt;
    }
}
