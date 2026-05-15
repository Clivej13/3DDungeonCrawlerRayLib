using DungeonCrawler.Player;
using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Rendering;

public sealed class RaycastRenderer
{
    private readonly DungeonMap _map;
    private readonly Texture2D _wallTexture;

    private readonly float _fov = MathF.PI / 3.2f;
    private readonly float _maxRayDistance = 1200f;

    // Low internal software resolution (retro + performance).
    private const int InternalWidth = 320;
    private const int InternalHeight = 200;

    // CPU framebuffer uploaded once per frame -> minimizes draw calls.
    private readonly Color[] _framebuffer;
    private readonly Texture2D _frameTexture;
    private readonly Color[] _wallPixels;
    private readonly int _wallWidth;
    private readonly int _wallHeight;

    public RaycastRenderer(DungeonMap map, Texture2D wallTexture)
    {
        _map = map;
        _wallTexture = wallTexture;

        _framebuffer = new Color[InternalWidth * InternalHeight];
        _frameTexture = Raylib.LoadTextureFromImage(Raylib.GenImageColor(InternalWidth, InternalHeight, Color.Black));
        Raylib.SetTextureFilter(_frameTexture, TextureFilter.Point);

        Image wallImage = Raylib.LoadImageFromTexture(_wallTexture);

        _wallWidth = wallImage.Width;
        _wallHeight = wallImage.Height;

        unsafe
        {
            Color* pixels = Raylib.LoadImageColors(wallImage);

            _wallPixels = new Color[_wallWidth * _wallHeight];

            for (int i = 0; i < _wallPixels.Length; i++)
            {
                _wallPixels[i] = pixels[i];
            }

            Raylib.UnloadImageColors(pixels);
        }

    Raylib.UnloadImage(wallImage);
    }

    public void Draw(PlayerController player)
    {
        int windowW = Raylib.GetScreenWidth();
        int windowH = Raylib.GetScreenHeight();
        int horizon = (InternalHeight / 2) + (int)(player.PitchOffset * (InternalHeight / (float)windowH));

        DrawFloor(player, horizon);
        DrawCeiling(player, horizon);
        DrawWalls(player, horizon);
        DrawCrosshair();

        Raylib.UpdateTexture(_frameTexture, _framebuffer);

        Raylib.DrawTexturePro(
            _frameTexture,
            new Rectangle(0, 0, InternalWidth, InternalHeight),
            new Rectangle(0, 0, windowW, windowH),
            Vector2.Zero,
            0f,
            Color.White);
    }

    private void DrawFloor(PlayerController player, int horizon)
        => DrawHorizontalPlane(player, horizon, Math.Clamp(horizon + 1, 0, InternalHeight), InternalHeight, true);

    private void DrawCeiling(PlayerController player, int horizon)
        => DrawHorizontalPlane(player, horizon, 0, Math.Clamp(horizon, 0, InternalHeight), false);

    private void DrawHorizontalPlane(PlayerController player, int horizon, int startY, int endYExclusive, bool isFloor)
    {
        float halfFov = _fov * 0.5f;
        float tanHalfFov = MathF.Tan(halfFov);
        float projPlaneDist = (InternalWidth * 0.5f) / tanHalfFov;

        Vector2 forward = new(MathF.Cos(player.Angle), MathF.Sin(player.Angle));
        Vector2 right = new(-forward.Y, forward.X);
        Vector2 leftRay = forward - right * tanHalfFov;
        Vector2 rightRay = forward + right * tanHalfFov;

        float cameraHeight = DungeonMap.TileSize * 0.5f;
        float planeHeightDelta = isFloor ? cameraHeight : (DungeonMap.TileSize - cameraHeight);

        float shadeMin = isFloor ? 0.18f : 0.10f;
        float shadeMax = isFloor ? 0.68f : 0.36f;
        float falloff = isFloor ? _maxRayDistance : _maxRayDistance * 0.85f;

        for (int y = startY; y < endYExclusive; y++)
        {
            float rowOffset = isFloor ? (y - horizon) : (horizon - y);
            if (rowOffset <= 0.001f) continue;

            float rowDistance = (planeHeightDelta * projPlaneDist) / rowOffset;
            float stepX = rowDistance * (rightRay.X - leftRay.X) / InternalWidth;
            float stepY = rowDistance * (rightRay.Y - leftRay.Y) / InternalWidth;

            float worldX = player.Position.X + rowDistance * leftRay.X;
            float worldY = player.Position.Y + rowDistance * leftRay.Y;

            byte shade = (byte)(Math.Clamp(1f - rowDistance / falloff, shadeMin, shadeMax) * 255);
            int rowIndex = y * InternalWidth;

            for (int x = 0; x < InternalWidth; x++)
            {
                int tx = WorldToTileTexel(worldX, DungeonMap.TileSize, _wallWidth);
                int ty = WorldToTileTexel(worldY, DungeonMap.TileSize, _wallHeight);

                Color c = _wallPixels[(ty * _wallWidth) + tx];
                _framebuffer[rowIndex + x] = Modulate(c, shade);

                worldX += stepX;
                worldY += stepY;
            }
        }
    }

    private void DrawWalls(PlayerController player, int horizon)
    {
        float halfFov = _fov * 0.5f;
        float projPlaneDist = (InternalWidth * 0.5f) / MathF.Tan(halfFov);

        for (int x = 0; x < InternalWidth; x++)
        {
            float cameraX = (2f * x / InternalWidth) - 1f;
            float rayAngle = player.Angle + cameraX * halfFov;
            var hit = CastRay(player.Position, rayAngle);

            float correctedDist = MathF.Max(hit.Distance * MathF.Cos(rayAngle - player.Angle), 0.0001f);
            int sliceHeight = (int)((DungeonMap.TileSize / correctedDist) * projPlaneDist);
            int drawTop = horizon - (sliceHeight / 2);
            int drawBottom = drawTop + sliceHeight;

            byte shade = (byte)(255 * Math.Clamp(1f - (correctedDist / _maxRayDistance), 0.2f, 1f));
            if (hit.HitVertical) shade = (byte)(shade * 0.88f);

            int texX = Math.Clamp((int)hit.TextureX, 0, _wallWidth - 1);

            for (int y = Math.Max(0, drawTop); y < Math.Min(InternalHeight, drawBottom); y++)
            {
                float t = (y - drawTop) / (float)Math.Max(sliceHeight, 1);
                int texY = Math.Clamp((int)(t * _wallHeight), 0, _wallHeight - 1);
                Color c = _wallPixels[(texY * _wallWidth) + texX];
                _framebuffer[y * InternalWidth + x] = Modulate(c, shade);
            }
        }
    }

    private (float Distance, float TextureX, bool HitVertical) CastRay(Vector2 origin, float rayAngle)
    {
        Vector2 rayDir = new(MathF.Cos(rayAngle), MathF.Sin(rayAngle));
        int mapX = (int)(origin.X / DungeonMap.TileSize);
        int mapY = (int)(origin.Y / DungeonMap.TileSize);

        float deltaDistX = rayDir.X == 0f ? float.MaxValue : MathF.Abs(DungeonMap.TileSize / rayDir.X);
        float deltaDistY = rayDir.Y == 0f ? float.MaxValue : MathF.Abs(DungeonMap.TileSize / rayDir.Y);

        int stepX = rayDir.X < 0 ? -1 : 1;
        int stepY = rayDir.Y < 0 ? -1 : 1;

        float sideDistX = rayDir.X < 0
            ? (origin.X - mapX * DungeonMap.TileSize) / -rayDir.X
            : ((mapX + 1) * DungeonMap.TileSize - origin.X) / (rayDir.X == 0f ? 0.0001f : rayDir.X);

        float sideDistY = rayDir.Y < 0
            ? (origin.Y - mapY * DungeonMap.TileSize) / -rayDir.Y
            : ((mapY + 1) * DungeonMap.TileSize - origin.Y) / (rayDir.Y == 0f ? 0.0001f : rayDir.Y);

        bool hitVertical = false;
        float distance = 0f;

        while (distance < _maxRayDistance)
        {
            if (sideDistX < sideDistY)
            {
                distance = sideDistX;
                sideDistX += deltaDistX;
                mapX += stepX;
                hitVertical = true;
            }
            else
            {
                distance = sideDistY;
                sideDistY += deltaDistY;
                mapY += stepY;
                hitVertical = false;
            }

            if (_map.IsWallAtGrid(mapX, mapY)) break;
        }

        Vector2 hitPoint = origin + rayDir * distance;
        float textureCoord = hitVertical ? hitPoint.Y : hitPoint.X;
        float textureX = textureCoord % DungeonMap.TileSize;
        if (textureX < 0f) textureX += DungeonMap.TileSize;

        return (distance, (textureX / DungeonMap.TileSize) * _wallWidth, hitVertical);
    }

    private static Color Modulate(Color c, byte shade)
        => new(
    (byte)(c.R * shade / 255),
    (byte)(c.G * shade / 255),
    (byte)(c.B * shade / 255),
    (byte)255);

    private static int PositiveMod(int value, int modulus)
    {
        int m = value % modulus;
        return m < 0 ? m + modulus : m;
    }

    private static int WorldToTileTexel(float worldCoord, int tileSize, int textureSize)
    {
        int cellLocal = PositiveMod((int)MathF.Floor(worldCoord), tileSize);
        return (cellLocal * textureSize) / tileSize;
    }

    private void DrawCrosshair()
    {
        int cx = InternalWidth / 2;
        int cy = InternalHeight / 2;
        DrawLineCPU(cx - 8, cy, cx + 8, cy, new Color(220, 220, 220, 180));
        DrawLineCPU(cx, cy - 8, cx, cy + 8, new Color(220, 220, 220, 180));
    }

    private void DrawLineCPU(int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Math.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            if ((uint)x0 < InternalWidth && (uint)y0 < InternalHeight)
            {
                _framebuffer[y0 * InternalWidth + x0] = color;
            }

            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    public void DrawMinimap(PlayerController player)
    {
        const int cell = 16;
        const int offsetX = 16;
        const int offsetY = 16;

        for (int y = 0; y < _map.Height; y++)
        {
            for (int x = 0; x < _map.Width; x++)
            {
                bool wall = _map.IsWallAtGrid(x, y);
                Raylib.DrawRectangle(offsetX + x * cell, offsetY + y * cell, cell - 1, cell - 1,
                    wall ? new Color(84, 86, 90, 220) : new Color(32, 36, 40, 170));
            }
        }

        float px = offsetX + (player.Position.X / DungeonMap.TileSize) * cell;
        float py = offsetY + (player.Position.Y / DungeonMap.TileSize) * cell;

        Vector2 forward = new(MathF.Cos(player.Angle), MathF.Sin(player.Angle));
        Vector2 right = new(-forward.Y, forward.X);
        float length = 8f;
        float baseOffset = 4f;
        float halfWidth = 4f;

        Vector2 tip = new(px, py) + forward * length;
        Vector2 left = new(px, py) - forward * baseOffset - right * halfWidth;
        Vector2 rightPoint = new(px, py) - forward * baseOffset + right * halfWidth;

        Raylib.DrawTriangle(tip, left, rightPoint, new Color(64, 196, 255, 255));
    }
}
