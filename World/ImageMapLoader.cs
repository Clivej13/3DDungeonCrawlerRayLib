using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.World;

public enum MapPixelType
{
    Floor,
    Wall,
    PlayerSpawn,
    Exit
}

public sealed class ImageMapData
{
    public required int[,] Grid { get; init; }
    public required Vector2 PlayerSpawn { get; init; }
    public ExitData? Exit { get; init; }
}

public static class ImageMapLoader
{
    public static bool ColorsEqual(Color a, Color b)
        => a.R == b.R && a.G == b.G && a.B == b.B;

    public static unsafe ImageMapData Load(string mapPath, int tileSize)
    {
        if (!File.Exists(mapPath))
        {
            throw new FileNotFoundException($"[MapValidation] PNG map file not found: '{mapPath}'.", mapPath);
        }

        Image image = Raylib.LoadImage(mapPath);

        try
        {
            Color* pixels = Raylib.LoadImageColors(image);

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

    private static unsafe ImageMapData ParsePixels(Color* pixels, int width, int height, int tileSize)
    {
        int[,] grid = new int[height, width];
        Vector2? playerSpawn = null;
        ExitData? exit = null;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = pixels[y * width + x];
                MapPixelType pixelType = ResolvePixelType(pixel);
                Vector2 world = MapLoader.ToWorldPosition(x, y, tileSize);

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
                }
            }
        }

        if (playerSpawn is null)
        {
            throw new InvalidOperationException("[MapValidation] Map is missing a player spawn (red pixel #FF0000).");
        }

        return new ImageMapData
        {
            Grid = grid,
            PlayerSpawn = playerSpawn.Value,
            Exit = exit
        };
    }

    private static MapPixelType ResolvePixelType(Color pixel)
    {
        if (ColorsEqual(pixel, MapColors.Wall)) return MapPixelType.Wall;
        if (ColorsEqual(pixel, MapColors.Floor)) return MapPixelType.Floor;
        if (ColorsEqual(pixel, MapColors.PlayerSpawn)) return MapPixelType.PlayerSpawn;
        if (ColorsEqual(pixel, MapColors.Exit)) return MapPixelType.Exit;

        return MapPixelType.Floor;
    }
}
