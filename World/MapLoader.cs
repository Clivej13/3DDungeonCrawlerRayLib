using DungeonCrawler.Entities;
using Raylib_cs;

namespace DungeonCrawler.World;

public static class MapLoader
{
    public static float TileToWorld(int tileCoordinate, int tileSize)
        => tileCoordinate * tileSize + tileSize / 2f;

    public static System.Numerics.Vector2 ToWorldPosition(int tileX, int tileY, int tileSize)
        => new(TileToWorld(tileX, tileSize), TileToWorld(tileY, tileSize));
    public static bool TryParseKeyType(string value, out KeyType keyType)
    {
        if (value.Equals("silver", StringComparison.OrdinalIgnoreCase))
        {
            keyType = KeyType.Silver;
            return true;
        }

        if (value.Equals("gold", StringComparison.OrdinalIgnoreCase))
        {
            keyType = KeyType.Gold;
            return true;
        }

        keyType = KeyType.Silver;
        return false;
    }

    public static List<Enemy> BuildEnemies(IEnumerable<EnemySpawnData> spawns, Texture2D goblinTexture)
    {
        List<Enemy> enemies = [];
        foreach (EnemySpawnData spawn in spawns)
        {
            if (spawn.Type.Equals("goblin", StringComparison.OrdinalIgnoreCase))
            {
                enemies.Add(new GoblinEnemy(new System.Numerics.Vector2(spawn.X, spawn.Y), goblinTexture));
            }
        }

        return enemies;
    }

    public static bool IsWalkableSpawn(int[,] grid, int tileSize, float worldX, float worldY)
    {
        int gridX = (int)MathF.Floor(worldX / tileSize);
        int gridY = (int)MathF.Floor(worldY / tileSize);
        if (gridX < 0 || gridY < 0 || gridY >= grid.GetLength(0) || gridX >= grid.GetLength(1))
        {
            return false;
        }

        return grid[gridY, gridX] == 0;
    }
}
