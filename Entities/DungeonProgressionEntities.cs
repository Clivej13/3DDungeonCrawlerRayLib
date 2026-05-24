using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Entities;

public sealed class KeyItem : Entity
{
    public string Id { get; }
    public Texture2D Texture { get; }
    public bool IsCollected { get; private set; }

    public KeyItem(Vector2 position, string id, Texture2D texture) : base(position)
    {
        Id = id;
        Texture = texture;
    }

    public void Collect() => IsCollected = true;
}

public sealed class DoorEntity : Entity
{
    public string Id { get; }
    public string? RequiredKeyId { get; }
    public DoorState State { get; private set; }
    public bool IsLocked => State == DoorState.Locked;
    public bool BlocksMovement => State is DoorState.Closed or DoorState.Locked or DoorState.Closing;
    public bool BlocksRaycast => BlocksMovement;

    public DoorEntity(Vector2 position, string id, string? requiredKeyId, bool isLocked) : base(position)
    {
        Id = id;
        RequiredKeyId = requiredKeyId;
        State = isLocked ? DoorState.Locked : DoorState.Closed;
    }

    public void Unlock() => State = DoorState.Closed;

    public void StartOpening()
    {
        State = DoorState.Opening;
        State = DoorState.Open;
    }

    public void StartClosing()
    {
        State = DoorState.Closing;
        State = DoorState.Closed;
    }
}
