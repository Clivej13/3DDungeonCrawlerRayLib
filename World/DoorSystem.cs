using DungeonCrawler.Entities;
using DungeonCrawler.Player;
using System.Numerics;

namespace DungeonCrawler.World;

public enum DoorState
{
    Closed,
    Opening,
    Open,
    Closing,
    Locked
}

public sealed class DoorSystem
{
    private readonly DungeonMap _map;
    private readonly float _interactionRange;
    private readonly float _autoCloseDelay;

    public DoorSystem(DungeonMap map, float interactionRange = 54f, float autoCloseDelay = 1.2f)
    {
        _map = map;
        _interactionRange = interactionRange;
        _autoCloseDelay = autoCloseDelay;
    }

    public DoorEntity? GetTargetedDoor(PlayerController player, float aimDotThreshold = 0.86f)
    {
        Vector2 forward = new(MathF.Cos(player.Angle), MathF.Sin(player.Angle));
        DoorEntity? bestDoor = null;
        float bestDistSq = float.MaxValue;

        foreach (DoorEntity door in _map.Doors)
        {
            Vector2 toDoor = door.Position - player.Position;
            float distSq = toDoor.LengthSquared();
            if (distSq > _interactionRange * _interactionRange || distSq <= 0.001f) continue;

            Vector2 dir = Vector2.Normalize(toDoor);
            float dot = Vector2.Dot(forward, dir);
            if (dot < aimDotThreshold) continue;

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestDoor = door;
            }
        }

        return bestDoor;
    }

    public void Update(float dt, PlayerController player, IReadOnlyCollection<Enemy> enemies)
    {
        foreach (DoorEntity door in _map.Doors)
        {
            door.Tick(dt);
            if (door.State != DoorState.Open) continue;
            if (door.TimeSinceOpened < _autoCloseDelay) continue;
            if (IsDoorwayBlocked(door, player, enemies)) continue;
            door.StartClosing();
        }
    }

    public bool TryInteract(PlayerController player, DoorEntity door, out string? status)
    {
        status = null;
        if (door.State == DoorState.Open) return false;

        if (door.State == DoorState.Locked)
        {
            bool hasKey = door.Type == KeyType.Silver ? player.HasSilverKey : player.HasGoldKey;
            if (!hasKey)
            {
                status = door.Type == KeyType.Silver ? "Need Silver Key" : "Need Gold Key";
                return false;
            }

            door.Unlock();
            status = "Door Unlocked";
        }

        if (door.State == DoorState.Closed)
        {
            door.StartOpening();
            return true;
        }

        return false;
    }

    private static bool IsDoorwayBlocked(DoorEntity door, PlayerController player, IReadOnlyCollection<Enemy> enemies)
    {
        float blockRadius = DungeonMap.TileSize * 0.28f;
        float radiusSq = blockRadius * blockRadius;

        if (Vector2.DistanceSquared(player.Position, door.Position) <= radiusSq) return true;
        return enemies.Any(e => e.IsAlive && Vector2.DistanceSquared(e.Position, door.Position) <= radiusSq);
    }
}
