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

    public DungeonMap(string levelMetadataPath, Texture2D goblinTexture, Texture2D goblinLightWindupTexture, Texture2D goblinHeavyWindupTexture, Texture2D goblinStrikeTexture, Texture2D goblinStaggerTexture, Texture2D keyTexture)
    {
        LevelMetadata metadata = LevelMetadataLoader.Load(levelMetadataPath, TileSize);
        ImageMapData geometry = ImageMapLoader.Load(metadata.MapImage, TileSize);

        _grid = geometry.Grid;
        PlayerSpawn = geometry.PlayerSpawn;
        PlayerSpawnAngle = 0f;
        Enemies = MapLoader.BuildEnemies(metadata.Enemies, goblinTexture, goblinLightWindupTexture, goblinHeavyWindupTexture, goblinStrikeTexture, goblinStaggerTexture);
        Exit = geometry.Exit;

        foreach (KeySpawnData spawn in metadata.Keys)
        {
            if (!MapLoader.IsWalkableSpawn(_grid, TileSize, spawn.X, spawn.Y))
            {
                Console.WriteLine($"[MapValidation] Key '{spawn.Id}' at ({spawn.X},{spawn.Y}) is inside wall/out of bounds. Skipped.");
                continue;
            }

            Keys.Add(new KeyItem(new Vector2(spawn.X, spawn.Y), spawn.Id, keyTexture));
        }

        foreach (DoorSpawnData spawn in metadata.Doors)
        {
            if (!MapLoader.IsWalkableSpawn(_grid, TileSize, spawn.X, spawn.Y))
            {
                Console.WriteLine($"[MapValidation] Door '{spawn.Id}' at ({spawn.X},{spawn.Y}) is inside wall/out of bounds. Skipped.");
                continue;
            }

            Doors.Add(new DoorEntity(new Vector2(spawn.X, spawn.Y), spawn.Id, spawn.RequiredKeyId, spawn.Locked));
        }

        if (Exit is null)
        {
            Console.WriteLine("[MapValidation] Map has no exit marker (green pixel #00FF00).");
        }
        else if (!MapLoader.IsWalkableSpawn(_grid, TileSize, Exit.X, Exit.Y))
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

        const float closedDoorCollisionRadius = TileSize * 0.46f;
        foreach (DoorEntity door in Doors.Where(d => d.BlocksMovement))
        {
            if (Vector2.DistanceSquared(new Vector2(worldX, worldY), door.Position) <= closedDoorCollisionRadius * closedDoorCollisionRadius)
            {
                return true;
            }
        }

        return false;
    }
}
