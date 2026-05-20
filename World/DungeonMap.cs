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
    public List<NodeConnectionData> NodeConnections { get; } = [];

    public DungeonMap(string mapPath, Texture2D goblinTexture, Texture2D silverKeyTexture, Texture2D goldKeyTexture)
    {
        DungeonMapData data = MapLoader.LoadData(mapPath);
        _grid = MapLoader.BuildGrid(data.Rows);
        PlayerSpawn = new Vector2(data.PlayerSpawn.X, data.PlayerSpawn.Y);
        PlayerSpawnAngle = data.PlayerSpawn.Angle;
        Enemies = MapLoader.BuildEnemies(data.Enemies, goblinTexture);
        Exit = data.Exit;
        BuildProgressionData(data.Keys, data.Doors, silverKeyTexture, goldKeyTexture);
    }

    private DungeonMap(NodeData data, Texture2D goblinTexture, Texture2D silverKeyTexture, Texture2D goldKeyTexture)
    {
        _grid = MapLoader.BuildGrid(data.Tiles);
        float spawnX = TileSize * 1.5f;
        float spawnY = TileSize * 1.5f;
        float spawnAngle = 0f;
        if (data.PlayerSpawn is not null)
        {
            spawnX = data.PlayerSpawn.X;
            spawnY = data.PlayerSpawn.Y;
            spawnAngle = data.PlayerSpawn.Angle;
        }

        PlayerSpawn = new Vector2(spawnX, spawnY);
        PlayerSpawnAngle = spawnAngle;
        Enemies = [];
        Exit = null;
        NodeConnections = data.Connections;

        BuildNodeObjects(data.Objects, goblinTexture, silverKeyTexture, goldKeyTexture);
    }

    public static DungeonMap FromNode(NodeData data, Texture2D goblinTexture, Texture2D silverKeyTexture, Texture2D goldKeyTexture)
        => new(data, goblinTexture, silverKeyTexture, goldKeyTexture);

    private void BuildProgressionData(IEnumerable<KeySpawnData> keySpawns, IEnumerable<DoorSpawnData> doorSpawns, Texture2D silverKeyTexture, Texture2D goldKeyTexture)
    {
        foreach (KeySpawnData spawn in keySpawns)
        {
            if (!MapLoader.TryParseKeyType(spawn.Type, out KeyType keyType)) continue;
            if (!MapLoader.IsWalkableSpawn(_grid, TileSize, spawn.X, spawn.Y)) continue;
            Keys.Add(new KeyItem(new Vector2(spawn.X, spawn.Y), keyType, keyType == KeyType.Silver ? silverKeyTexture : goldKeyTexture));
        }

        foreach (DoorSpawnData spawn in doorSpawns)
        {
            if (!MapLoader.TryParseKeyType(spawn.Type, out KeyType keyType)) continue;
            if (!MapLoader.IsWalkableSpawn(_grid, TileSize, spawn.X, spawn.Y)) continue;
            Doors.Add(new DoorEntity(new Vector2(spawn.X, spawn.Y), keyType, spawn.Locked));
        }
    }

    private void BuildNodeObjects(IEnumerable<NodeObjectData> objects, Texture2D goblinTexture, Texture2D silverKeyTexture, Texture2D goldKeyTexture)
    {
        foreach (NodeObjectData obj in objects)
        {
            if (obj.Type.Equals("goblin", StringComparison.OrdinalIgnoreCase))
            {
                Enemies.Add(new GoblinEnemy(new Vector2(obj.X, obj.Y), goblinTexture));
            }
            else if (MapLoader.TryParseKeyType(obj.Type, out KeyType keyType))
            {
                Keys.Add(new KeyItem(new Vector2(obj.X, obj.Y), keyType, keyType == KeyType.Silver ? silverKeyTexture : goldKeyTexture));
            }
        }
    }

    public bool TryGetNodeConnectionAtWorld(float worldX, float worldY, out NodeConnectionData? connection)
    {
        int gx = (int)(worldX / TileSize);
        int gy = (int)(worldY / TileSize);
        connection = NodeConnections.FirstOrDefault(c => c.TileX == gx && c.TileY == gy);
        return connection is not null;
    }

    public bool IsWallAtGrid(int gx, int gy)
    {
        if (gx < 0 || gy < 0 || gx >= Width || gy >= Height) return true;
        return _grid[gy, gx] == 1;
    }

    public bool IsWallAtWorld(float worldX, float worldY)
    {
        int gx = (int)(worldX / TileSize);
        int gy = (int)(worldY / TileSize);
        return IsWallAtGrid(gx, gy);
    }

    public DoorEntity? GetDoorAtGrid(int gx, int gy)
        => Doors.FirstOrDefault(d => (int)(d.Position.X / TileSize) == gx && (int)(d.Position.Y / TileSize) == gy);

    public bool IsBlockedAtWorld(float worldX, float worldY)
    {
        if (IsWallAtWorld(worldX, worldY)) return true;
        const float doorRadius = 16f;
        foreach (DoorEntity door in Doors.Where(d => d.BlocksMovement))
        {
            if (Vector2.DistanceSquared(new Vector2(worldX, worldY), door.Position) <= doorRadius * doorRadius) return true;
        }

        return false;
    }
}
