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

    public DungeonMap(string mapPath, Texture2D goblinTexture)
    {
        DungeonMapData data = MapLoader.LoadData(mapPath);
        _grid = MapLoader.BuildGrid(data.Rows);
        PlayerSpawn = new Vector2(data.PlayerSpawn.X, data.PlayerSpawn.Y);
        PlayerSpawnAngle = data.PlayerSpawn.Angle;
        Enemies = MapLoader.BuildEnemies(data.Enemies, goblinTexture);
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
}
