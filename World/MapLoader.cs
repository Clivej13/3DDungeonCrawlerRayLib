using DungeonCrawler.Entities;
using Raylib_cs;

namespace DungeonCrawler.World;

public static class MapLoader
{
    public static float TileToWorld(int tileCoordinate, int tileSize)
        => tileCoordinate * tileSize + tileSize / 2f;

    public static System.Numerics.Vector2 ToWorldPosition(int tileX, int tileY, int tileSize)
        => new(TileToWorld(tileX, tileSize), TileToWorld(tileY, tileSize));
    public static List<Enemy> BuildEnemies(IEnumerable<EnemySpawnData> spawns, Texture2D goblinTexture, Texture2D goblinWindupTexture, Texture2D goblinAttackTexture)
    {
        List<Enemy> enemies = [];
        foreach (EnemySpawnData spawn in spawns)
        {
            if (spawn.Type.Equals("goblin", StringComparison.OrdinalIgnoreCase))
            {
                enemies.Add(new GoblinEnemy(new System.Numerics.Vector2(spawn.X, spawn.Y), goblinTexture, goblinWindupTexture, goblinAttackTexture));
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
