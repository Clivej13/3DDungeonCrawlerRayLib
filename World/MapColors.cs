using Raylib_cs;

namespace DungeonCrawler.World;

public static class MapColors
{
    public static readonly Color Wall = new(0, 0, 0, 255);
    public static readonly Color Floor = new(255, 255, 255, 255);
    public static readonly Color PlayerSpawn = new(255, 0, 0, 255);
    public static readonly Color Exit = new(0, 255, 0, 255);
    public static readonly Color Goblin = new(0, 0, 255, 255);
    public static readonly Color GoldKey = new(255, 255, 0, 255);
    public static readonly Color SilverKey = new(192, 192, 192, 255);
    public static readonly Color GoldDoor = new(139, 69, 19, 255);
    public static readonly Color SilverDoor = new(112, 128, 144, 255);
}
