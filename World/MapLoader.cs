using Raylib_cs;
using DungeonCrawler.Entities;
using System.Text.Json;

namespace DungeonCrawler.World;

public static class MapLoader
{
    public static DungeonMapData LoadData(string mapPath)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        string json = File.ReadAllText(mapPath);
        DungeonMapData? data = JsonSerializer.Deserialize<DungeonMapData>(json, options);

        if (data is null || data.Rows.Length == 0)
        {
            throw new InvalidOperationException($"Failed to load map data from '{mapPath}'.");
        }

        return data;
    }

    public static int[,] BuildGrid(string[] rows)
    {
        int height = rows.Length;
        int width = rows[0].Length;
        var grid = new int[height, width];

        for (int y = 0; y < height; y++)
        {
            if (rows[y].Length != width)
            {
                throw new InvalidOperationException("Map rows must have equal width.");
            }

            for (int x = 0; x < width; x++)
            {
                grid[y, x] = rows[y][x] == '1' ? 1 : 0;
            }
        }

        return grid;
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
}
