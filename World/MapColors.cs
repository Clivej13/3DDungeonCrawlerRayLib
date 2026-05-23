using Raylib_cs;

namespace DungeonCrawler.World;

public static class MapColors
{
    public static readonly Color Wall = new(0, 0, 0, 255);
    public static readonly Color Floor = new(255, 255, 255, 255);
    public static readonly Color PlayerSpawn = new(255, 0, 0, 255);
    public static readonly Color Exit = new(0, 255, 0, 255);
}
