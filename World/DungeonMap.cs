namespace DungeonCrawler.World;

public sealed class DungeonMap
{
    public const int TileSize = 64;

    private readonly int[,] _grid =
    {
        {1,1,1,1,1,1,1,1},
        {1,0,0,0,0,0,0,1},
        {1,0,1,1,0,1,0,1},
        {1,0,0,0,0,1,0,1},
        {1,1,1,1,1,1,1,1}
    };

    public int Width => _grid.GetLength(1);
    public int Height => _grid.GetLength(0);

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
