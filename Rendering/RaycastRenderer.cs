using DungeonCrawler.Player;
using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Rendering;

public sealed class RaycastRenderer
{
    private readonly DungeonMap _map;
    private readonly Texture2D _wallTexture;

    private readonly float _fov = MathF.PI / 3.2f; // ~56 deg; retro tight FOV.
    private readonly float _maxRayDistance = 1200f;

    public RaycastRenderer(DungeonMap map, Texture2D wallTexture)
    {
        _map = map;
        _wallTexture = wallTexture;
    }

    public void Draw(PlayerController player)
    {
        int sw = Raylib.GetScreenWidth();
        int sh = Raylib.GetScreenHeight();
        int horizon = (sh / 2) + (int)player.PitchOffset;

        DrawFloorAndCeiling(player, sw, sh, horizon);
        DrawWalls(player, sw, sh, horizon);
        DrawCrosshair(sw, sh);
    }

    private void DrawFloorAndCeiling(PlayerController player, int sw, int sh, int horizon)
    {
        // Perspective-correct scanline floor casting.
        // For each floor row, compute distance once then step world-space coordinates across X.
        float halfFov = _fov * 0.5f;
        Vector2 forward = new(MathF.Cos(player.Angle), MathF.Sin(player.Angle));
        Vector2 right = new(-forward.Y, forward.X);
        float planeHalfWidth = MathF.Tan(halfFov);

        Vector2 leftRay = forward - right * planeHalfWidth;
        Vector2 rightRay = forward + right * planeHalfWidth;

        float cameraHeight = DungeonMap.TileSize * 0.5f;
        int floorStart = Math.Clamp(horizon + 1, 0, sh);
        int ceilingEnd = Math.Clamp(horizon, 0, sh - 1);

        for (int y = floorStart; y < sh; y++)
        {
            float rowOffset = y - horizon;
            if (rowOffset <= 0.001f) continue;

            float rowDistance = (cameraHeight * sh) / (2f * rowOffset);
            float stepX = rowDistance * (rightRay.X - leftRay.X) / sw;
            float stepY = rowDistance * (rightRay.Y - leftRay.Y) / sw;

            float worldX = player.Position.X + rowDistance * leftRay.X;
            float worldY = player.Position.Y + rowDistance * leftRay.Y;

            byte floorShade = (byte)(Math.Clamp(1f - rowDistance / _maxRayDistance, 0.18f, 0.68f) * 255);
            byte ceilShade = (byte)(Math.Clamp(1f - rowDistance / (_maxRayDistance * 0.85f), 0.10f, 0.36f) * 255);

            int ceilY = horizon - (y - horizon);

            for (int x = 0; x < sw; x++)
            {
                int tx = PositiveMod((int)worldX, DungeonMap.TileSize) * _wallTexture.Width / DungeonMap.TileSize;
                int ty = PositiveMod((int)worldY, DungeonMap.TileSize) * _wallTexture.Height / DungeonMap.TileSize;

                var src = new Rectangle(tx, ty, 1, 1);
                var floorDst = new Rectangle(x, y, 1, 1);
                Raylib.DrawTexturePro(_wallTexture, src, floorDst, Vector2.Zero, 0f, new Color(floorShade, floorShade, floorShade, (byte)255));

                if (ceilY >= 0 && ceilY <= ceilingEnd)
                {
                    var ceilDst = new Rectangle(x, ceilY, 1, 1);
                    Raylib.DrawTexturePro(_wallTexture, src, ceilDst, Vector2.Zero, 0f, new Color(ceilShade, ceilShade, ceilShade, (byte)255));
                }

                worldX += stepX;
                worldY += stepY;
            }
        }
    }

    private void DrawWalls(PlayerController player, int sw, int sh, int horizon)
    {
        float projPlaneDist = (sw * 0.5f) / MathF.Tan(_fov * 0.5f);

        for (int x = 0; x < sw; x++)
        {
            float cameraX = (2f * x / sw) - 1f;
            float rayAngle = player.Angle + cameraX * (_fov * 0.5f);
            var hit = CastRay(player.Position, rayAngle);

            float correctedDist = hit.Distance * MathF.Cos(rayAngle - player.Angle);
            correctedDist = MathF.Max(correctedDist, 0.0001f);

            float wallHeight = (DungeonMap.TileSize / correctedDist) * projPlaneDist;
            int sliceHeight = (int)wallHeight;
            int drawTop = horizon - (sliceHeight / 2);

            float shadeFactor = Math.Clamp(1f - (correctedDist / _maxRayDistance), 0.2f, 1f);
            byte shade = (byte)(255 * shadeFactor);
            if (hit.HitVertical) shade = (byte)(shade * 0.88f);

            var src = new Rectangle(hit.TextureX, 0, 1, _wallTexture.Height);
            var dst = new Rectangle(x, drawTop, 1, sliceHeight);
            Raylib.DrawTexturePro(_wallTexture, src, dst, Vector2.Zero, 0f, new Color(shade, shade, shade, (byte)255));
        }
    }

    private (float Distance, float TextureX, bool HitVertical) CastRay(Vector2 origin, float rayAngle)
    {
        Vector2 rayDir = new(MathF.Cos(rayAngle), MathF.Sin(rayAngle));

        int mapX = (int)(origin.X / DungeonMap.TileSize);
        int mapY = (int)(origin.Y / DungeonMap.TileSize);

        float deltaDistX = rayDir.X == 0f ? float.MaxValue : MathF.Abs(DungeonMap.TileSize / rayDir.X);
        float deltaDistY = rayDir.Y == 0f ? float.MaxValue : MathF.Abs(DungeonMap.TileSize / rayDir.Y);

        int stepX;
        int stepY;
        float sideDistX;
        float sideDistY;

        if (rayDir.X < 0)
        {
            stepX = -1;
            sideDistX = (origin.X - mapX * DungeonMap.TileSize) / -rayDir.X;
        }
        else
        {
            stepX = 1;
            sideDistX = ((mapX + 1) * DungeonMap.TileSize - origin.X) / (rayDir.X == 0f ? 0.0001f : rayDir.X);
        }

        if (rayDir.Y < 0)
        {
            stepY = -1;
            sideDistY = (origin.Y - mapY * DungeonMap.TileSize) / -rayDir.Y;
        }
        else
        {
            stepY = 1;
            sideDistY = ((mapY + 1) * DungeonMap.TileSize - origin.Y) / (rayDir.Y == 0f ? 0.0001f : rayDir.Y);
        }

        bool hitVertical = false;
        bool hit = false;
        float distance = 0f;

        while (!hit && distance < _maxRayDistance)
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

            if (_map.IsWallAtGrid(mapX, mapY))
            {
                hit = true;
            }
        }

        Vector2 hitPoint = origin + rayDir * distance;
        float textureCoord = hitVertical ? hitPoint.Y : hitPoint.X;
        float textureX = textureCoord % DungeonMap.TileSize;
        if (textureX < 0f) textureX += DungeonMap.TileSize;
        textureX = (textureX / DungeonMap.TileSize) * _wallTexture.Width;

        return (distance, textureX, hitVertical);
    }

    private static int PositiveMod(int value, int modulus)
    {
        int m = value % modulus;
        return m < 0 ? m + modulus : m;
    }

    private static void DrawCrosshair(int sw, int sh)
    {
        int cx = sw / 2;
        int cy = sh / 2;
        Raylib.DrawLine(cx - 8, cy, cx + 8, cy, new Color(220, 220, 220, 180));
        Raylib.DrawLine(cx, cy - 8, cx, cy + 8, new Color(220, 220, 220, 180));
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
        Raylib.DrawCircle((int)px, (int)py, 4, Color.Yellow);

        Vector2 dir = new(MathF.Cos(player.Angle), MathF.Sin(player.Angle));
        Raylib.DrawLine((int)px, (int)py, (int)(px + dir.X * 14), (int)(py + dir.Y * 14), Color.Orange);
    }
}
