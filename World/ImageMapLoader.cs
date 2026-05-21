using DungeonCrawler.Entities;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.World;

public enum MapPixelType
{
    Floor,
    Wall,
    PlayerSpawn,
    Exit,
    Goblin,
    GoldKey,
    SilverKey,
    GoldDoor,
    SilverDoor
}

public sealed class ImageMapData
{
    public required int[,] Grid { get; init; }
    public required Vector2 PlayerSpawn { get; init; }
    public required List<EnemySpawnData> Enemies { get; init; }
    public required List<KeySpawnData> Keys { get; init; }
    public required List<DoorSpawnData> Doors { get; init; }
    public ExitData? Exit { get; init; }
}

public static class ImageMapLoader
{
    public static bool ColorsEqual(Color a, Color b)
        => a.R == b.R && a.G == b.G && a.B == b.B;

    public static ImageMapData Load(string mapPath, int tileSize)
    {
        Image image = Raylib.LoadImage(mapPath);
        try
        {
            Color[] pixels = Raylib.LoadImageColors(image);
            try
            {
                return ParsePixels(pixels, image.Width, image.Height, tileSize);
            }
            finally
            {
                Raylib.UnloadImageColors(pixels);
            }
        }
        finally
        {
            Raylib.UnloadImage(image);
        }
    }

    private static ImageMapData ParsePixels(Color[] pixels, int width, int height, int tileSize)
    {
        int[,] grid = new int[height, width];
        List<EnemySpawnData> enemies = [];
        List<KeySpawnData> keys = [];
        List<DoorSpawnData> doors = [];

        Vector2? playerSpawn = null;
        ExitData? exit = null;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = pixels[y * width + x];
                MapPixelType pixelType = ResolvePixelType(pixel);
                Vector2 world = ToWorldPosition(x, y, tileSize);

                switch (pixelType)
                {
                    case MapPixelType.Wall:
                        grid[y, x] = 1;
                        break;
                    case MapPixelType.PlayerSpawn:
                        playerSpawn = world;
                        break;
                    case MapPixelType.Exit:
                        exit = new ExitData { X = world.X, Y = world.Y, RequiresAllKeys = true };
                        break;
                    case MapPixelType.Goblin:
                        enemies.Add(new EnemySpawnData { Type = "goblin", X = world.X, Y = world.Y });
                        break;
                    case MapPixelType.GoldKey:
                        keys.Add(new KeySpawnData { Type = "gold", X = world.X, Y = world.Y });
                        break;
                    case MapPixelType.SilverKey:
                        keys.Add(new KeySpawnData { Type = "silver", X = world.X, Y = world.Y });
                        break;
                    case MapPixelType.GoldDoor:
                        doors.Add(new DoorSpawnData { Type = "gold", X = world.X, Y = world.Y, Locked = true });
                        break;
                    case MapPixelType.SilverDoor:
                        doors.Add(new DoorSpawnData { Type = "silver", X = world.X, Y = world.Y, Locked = true });
                        break;
                    case MapPixelType.Floor:
                    default:
                        break;
                }
            }
        }

        if (playerSpawn is null)
        {
            throw new InvalidOperationException("Map is missing a player spawn (red pixel).");
        }

        return new ImageMapData
        {
            Grid = grid,
            PlayerSpawn = playerSpawn.Value,
            Enemies = enemies,
            Keys = keys,
            Doors = doors,
            Exit = exit
        };
    }

    private static Vector2 ToWorldPosition(int x, int y, int tileSize)
    {
        float worldX = x * tileSize + tileSize / 2f;
        float worldY = y * tileSize + tileSize / 2f;
        return new Vector2(worldX, worldY);
    }

    private static MapPixelType ResolvePixelType(Color pixel)
    {
        if (ColorsEqual(pixel, MapColors.Wall)) return MapPixelType.Wall;
        if (ColorsEqual(pixel, MapColors.Floor)) return MapPixelType.Floor;
        if (ColorsEqual(pixel, MapColors.PlayerSpawn)) return MapPixelType.PlayerSpawn;
        if (ColorsEqual(pixel, MapColors.Exit)) return MapPixelType.Exit;
        if (ColorsEqual(pixel, MapColors.Goblin)) return MapPixelType.Goblin;
        if (ColorsEqual(pixel, MapColors.GoldKey)) return MapPixelType.GoldKey;
        if (ColorsEqual(pixel, MapColors.SilverKey)) return MapPixelType.SilverKey;
        if (ColorsEqual(pixel, MapColors.GoldDoor)) return MapPixelType.GoldDoor;
        if (ColorsEqual(pixel, MapColors.SilverDoor)) return MapPixelType.SilverDoor;

        return MapPixelType.Floor;
    }
}
