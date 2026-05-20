using DungeonCrawler.Entities;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.World;

public sealed class DungeonMap
{
    public const int TileSize = 64;
    private readonly int[,] _grid;

    public int Width => _grid.GetLength(1);
    public int Height => _grid.GetLength(0);
    public Vector2 PlayerSpawn { get; }
    public float PlayerSpawnAngle { get; }
    public List<Enemy> Enemies { get; }
    public List<KeyItem> Keys { get; } = [];
    public List<DoorEntity> Doors { get; } = [];
    public ExitData? Exit { get; }

    public DungeonMap(string mapPath, Texture2D goblinTexture, Texture2D silverKeyTexture, Texture2D goldKeyTexture)
    {
        DungeonMapData data = MapLoader.LoadData(mapPath);
        _grid = MapLoader.BuildGrid(data.Rows);
        PlayerSpawn = new Vector2(data.PlayerSpawn.X, data.PlayerSpawn.Y);
        PlayerSpawnAngle = data.PlayerSpawn.Angle;
        Enemies = MapLoader.BuildEnemies(data.Enemies, goblinTexture);
        Exit = data.Exit;

        foreach (KeySpawnData spawn in data.Keys)
        {
            if (!MapLoader.TryParseKeyType(spawn.Type, out KeyType keyType))
            {
                Console.WriteLine($"[MapValidation] Unknown key type '{spawn.Type}'. Skipped.");
                continue;
            }

            if (!MapLoader.IsWalkableSpawn(_grid, TileSize, spawn.X, spawn.Y))
            {
                Console.WriteLine($"[MapValidation] Key {keyType} at ({spawn.X},{spawn.Y}) is inside wall/out of bounds. Skipped.");
                continue;
            }

            Keys.Add(new KeyItem(new Vector2(spawn.X, spawn.Y), keyType, keyType == KeyType.Silver ? silverKeyTexture : goldKeyTexture));
        }

        foreach (DoorSpawnData spawn in data.Doors)
        {
            if (!MapLoader.TryParseKeyType(spawn.Type, out KeyType keyType))
            {
                Console.WriteLine($"[MapValidation] Unknown door type '{spawn.Type}'. Skipped.");
                continue;
            }

            if (!MapLoader.IsWalkableSpawn(_grid, TileSize, spawn.X, spawn.Y))
            {
                Console.WriteLine($"[MapValidation] Door {keyType} at ({spawn.X},{spawn.Y}) is inside wall/out of bounds. Skipped.");
                continue;
            }

            Doors.Add(new DoorEntity(new Vector2(spawn.X, spawn.Y), keyType, spawn.Locked));
        }

        if (Exit is not null && !MapLoader.IsWalkableSpawn(_grid, TileSize, Exit.X, Exit.Y))
        {
            Console.WriteLine($"[MapValidation] Exit at ({Exit.X},{Exit.Y}) is inside wall/out of bounds.");
        }
    }

    public bool IsWallAtGrid(int gx, int gy)
    {
        if (gx < 0 || gy < 0 || gx >= Width || gy >= Height)
        {
            return true;
        }

        return _grid[gy, gx] == 1;
    }

    public bool IsWallAtWorld(float worldX, float worldY)
    {
        int gx = (int)(worldX / TileSize);
        int gy = (int)(worldY / TileSize);
        return IsWallAtGrid(gx, gy);
    }


    public DoorEntity? GetDoorAtGrid(int gx, int gy)
    {
        return Doors.FirstOrDefault(d => (int)(d.Position.X / TileSize) == gx && (int)(d.Position.Y / TileSize) == gy);
    }

    public bool IsBlockedAtWorld(float worldX, float worldY)
    {
        if (IsWallAtWorld(worldX, worldY)) return true;

        const float doorRadius = 16f;
        foreach (DoorEntity door in Doors.Where(d => d.BlocksMovement))
        {
            if (Vector2.DistanceSquared(new Vector2(worldX, worldY), door.Position) <= doorRadius * doorRadius)
            {
                return true;
            }
        }

        return false;
    }
}
