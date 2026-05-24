using DungeonCrawler.Entities;
using DungeonCrawler.Player;
using System.Numerics;

using DungeonCrawler.Core;

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
    private const float DoorCloseSafeDistanceMultiplier = 0.75f;
    private readonly DungeonMap _map;
    private readonly float _interactionRange;
    private readonly GameOptions _options;
    private readonly float _doorCloseSafeDistance;

    public DoorSystem(DungeonMap map, GameOptions options, float interactionRange = 96f)
    {
        _map = map;
        _options = options;
        _interactionRange = interactionRange;
        _doorCloseSafeDistance = DungeonMap.TileSize * DoorCloseSafeDistanceMultiplier;
    }

    public DoorEntity? GetTargetedDoor(PlayerController player, float aimDotThreshold = 0.73f)
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
        _ = dt;
        _ = enemies;
        _ = player;
    }

    public bool TryInteract(PlayerController player, DoorEntity door, out string? status)
    {
        status = null;
        if (door.State == DoorState.Open)
        {
            if (!CanDoorClose(door, player.Position))
            {
                status = "Too close to close door";
                return false;
            }

            door.StartClosing();
            return true;
        }

        if (door.State == DoorState.Locked)
        {
            if (string.IsNullOrWhiteSpace(door.RequiredKeyId))
            {
                door.Unlock();
            }
            else
            {
                // Debug mode can bypass key checks while keeping normal door/key flow intact.
                bool hasKey = (_options.DebugModeEnabled && _options.DisableKeyRequirements) || player.HasKey(door.RequiredKeyId);
                if (!hasKey)
                {
                    status = $"Need key: {door.RequiredKeyId}";
                    return false;
                }

                door.Unlock();
                status = "Door Unlocked";
            }
        }

        if (door.State == DoorState.Closed)
        {
            door.StartOpening();
            return true;
        }

        return false;
    }

    private bool CanDoorClose(DoorEntity door, Vector2 playerPosition)
    {
        float safeDistanceSq = _doorCloseSafeDistance * _doorCloseSafeDistance;
        return Vector2.DistanceSquared(playerPosition, door.Position) > safeDistanceSq;
    }
}
