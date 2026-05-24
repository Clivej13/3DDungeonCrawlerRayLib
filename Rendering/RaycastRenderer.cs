using DungeonCrawler.Entities;
using DungeonCrawler.Player;
using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Rendering;

public sealed class RaycastRenderer
{
    private enum RayHitType
    {
        Wall,
        Door
    }

    private readonly record struct RayHit(
        RayHitType Type,
        float Distance,
        float TextureX,
        bool HitVertical,
        bool BlocksView,
        (Color[] Pixels, int Width, int Height) Texture);

    private readonly DungeonMap _map;
    private readonly Texture2D _wallTexture;
    private readonly float[] _depthBuffer;
    private readonly Dictionary<uint, (Color[] Pixels, int Width, int Height)> _spriteCache = [];

    private readonly float _fov = MathF.PI / 3.2f;
    private readonly float _maxRayDistance = 1200f;

    private const int InternalWidth = 320;
    private const int InternalHeight = 200;

    private const float AtmosphereDistanceScale = 700f;
    private const float WallMinBrightness = 0.05f;
    private const float DoorMinBrightness = 0.06f;
    private const float SpriteMinBrightness = 0.08f;

    private readonly Color[] _framebuffer;
    private readonly Texture2D _frameTexture;
    private readonly Color[] _wallPixels;
    private readonly int _wallWidth;
    private readonly int _wallHeight;
    private readonly Texture2D _closedDoorTexture;
    private readonly Texture2D _openDoorTexture;
    private readonly Texture2D _minimapKeyTexture;
    private readonly Texture2D _minimapLockTexture;
    private readonly Texture2D _minimapTickTexture;

    public RaycastRenderer(
        DungeonMap map,
        Texture2D wallTexture,
        Texture2D closedDoorTexture,
        Texture2D openDoorTexture,
        Texture2D minimapLockTexture,
        Texture2D minimapTickTexture)
    {
        _map = map;
        _wallTexture = wallTexture;
        _closedDoorTexture = closedDoorTexture;
        _openDoorTexture = openDoorTexture;
        _minimapLockTexture = minimapLockTexture;
        _minimapTickTexture = minimapTickTexture;
        _minimapKeyTexture = _map.Keys.FirstOrDefault()?.Texture ?? openDoorTexture;

        _framebuffer = new Color[InternalWidth * InternalHeight];
        _depthBuffer = new float[InternalWidth];
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
        DrawWorld(player, horizon);
        DrawEnemies(player, horizon);
        DrawKeys(player, horizon);
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

        float shadeMin = isFloor ? 0.10f : 0.06f;
        float shadeMax = isFloor ? 0.54f : 0.30f;
        float falloff = isFloor ? AtmosphereDistanceScale * 0.62f : AtmosphereDistanceScale * 0.52f;

        for (int y = startY; y < endYExclusive; y++)
        {
            float rowOffset = isFloor ? (y - horizon) : (horizon - y);
            if (rowOffset <= 0.001f) continue;

            float rowDistance = (planeHeightDelta * projPlaneDist) / rowOffset;
            float stepX = rowDistance * (rightRay.X - leftRay.X) / InternalWidth;
            float stepY = rowDistance * (rightRay.Y - leftRay.Y) / InternalWidth;

            float worldX = player.Position.X + rowDistance * leftRay.X;
            float worldY = player.Position.Y + rowDistance * leftRay.Y;

            float distanceShade = DistanceShade(rowDistance, shadeMin, shadeMax, falloff);
            if (isFloor) distanceShade *= 0.84f;
            else distanceShade *= 0.90f;
            byte shade = (byte)(Math.Clamp(distanceShade, shadeMin, shadeMax) * 255);
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

    private void DrawWorld(PlayerController player, int horizon)
    {
        float halfFov = _fov * 0.5f;
        float projPlaneDist = (InternalWidth * 0.5f) / MathF.Tan(halfFov);

        for (int x = 0; x < InternalWidth; x++)
        {
            float cameraX = (2f * x / InternalWidth) - 1f;
            float rayAngle = player.Angle + cameraX * halfFov;
            List<RayHit> hits = CastRay(player.Position, rayAngle);
            if (hits.Count == 0)
            {
                _depthBuffer[x] = _maxRayDistance;
                continue;
            }

            _depthBuffer[x] = _maxRayDistance;
            foreach (RayHit hit in hits)
            {
                if (hit.BlocksView)
                {
                    _depthBuffer[x] = MathF.Max(hit.Distance * MathF.Cos(rayAngle - player.Angle), 0.0001f);
                    break;
                }
            }

            for (int i = hits.Count - 1; i >= 0; i--)
            {
                DrawWorldSlice(hits[i], rayAngle, player.Angle, horizon, projPlaneDist, x);
            }
        }
    }

    private void DrawWorldSlice(RayHit hit, float rayAngle, float playerAngle, int horizon, float projPlaneDist, int screenX)
    {
        float correctedDist = MathF.Max(hit.Distance * MathF.Cos(rayAngle - playerAngle), 0.0001f);
        int sliceHeight = (int)((DungeonMap.TileSize / correctedDist) * projPlaneDist);
        int drawTop = horizon - (sliceHeight / 2);
        int drawBottom = drawTop + sliceHeight;

        float maxBrightness = hit.Type == RayHitType.Wall ? 0.74f : 0.80f;
        float minBrightness = hit.Type == RayHitType.Wall ? WallMinBrightness : DoorMinBrightness;
        float falloff = hit.Type == RayHitType.Wall ? AtmosphereDistanceScale * 0.85f : AtmosphereDistanceScale * 0.9f;
        byte shade = ShadeByte(correctedDist, minBrightness, maxBrightness, falloff);
        shade = hit.HitVertical ? (byte)(shade * 0.80f) : (byte)(shade * 0.90f);

        int texX = Math.Clamp((int)hit.TextureX, 0, hit.Texture.Width - 1);
        for (int y = Math.Max(0, drawTop); y < Math.Min(InternalHeight, drawBottom); y++)
        {
            float t = (y - drawTop) / (float)Math.Max(sliceHeight, 1);
            int texY = Math.Clamp((int)(t * hit.Texture.Height), 0, hit.Texture.Height - 1);
            Color c = hit.Texture.Pixels[(texY * hit.Texture.Width) + texX];
            if (c.A < 10) continue;
            _framebuffer[y * InternalWidth + screenX] = Modulate(c, shade);
        }
    }


    private void DrawEnemies(PlayerController player, int horizon)
    {
        float halfFov = _fov * 0.5f;
        float invDet; // camera inverse determinant for world->camera transform

        Vector2 forward = new(MathF.Cos(player.Angle), MathF.Sin(player.Angle));
        Vector2 right = new(-forward.Y, forward.X);

        Vector2 plane = right * MathF.Tan(halfFov);
        invDet = 1f / ((plane.X * forward.Y) - (forward.X * plane.Y));
        float projPlaneDist = (InternalWidth * 0.5f) / MathF.Tan(halfFov);

        foreach (Enemy enemy in _map.Enemies.Where(e => e.IsAlive).OrderByDescending(e => e.DistanceToPlayer))
        {
            Vector2 rel = enemy.Position - player.Position;
            float transformX = invDet * ((forward.Y * rel.X) - (forward.X * rel.Y));
            float transformY = invDet * ((-plane.Y * rel.X) + (plane.X * rel.Y));
            if (transformY <= 0.001f) continue;

            float angleToEnemy = MathF.Atan2(rel.Y, rel.X) - player.Angle;
            angleToEnemy = MathF.Atan2(MathF.Sin(angleToEnemy), MathF.Cos(angleToEnemy));
            if (MathF.Abs(angleToEnemy) > halfFov) continue;

            int spriteScreenX = (int)((InternalWidth * 0.5f) * (1f + (transformX / transformY)));
            int spriteHeight = Math.Max(1, (int)(DungeonMap.TileSize * projPlaneDist / transformY * 0.50f));
            int spriteWidth = spriteHeight;
            int drawBottom = horizon + spriteHeight;
            int drawTop = drawBottom - spriteHeight;
            int drawLeft = spriteScreenX - (spriteWidth / 2);
            int drawRight = drawLeft + spriteWidth;

            var sprite = GetSpritePixels(enemy.CurrentTexture);
            byte shade = ShadeByte(transformY, SpriteMinBrightness, 0.85f, AtmosphereDistanceScale * 0.9f);

            for (int screenX = Math.Max(0, drawLeft); screenX < Math.Min(InternalWidth, drawRight); screenX++)
            {
                if (transformY >= _depthBuffer[screenX]) continue;

                int texX = (int)((screenX - drawLeft) / (float)spriteWidth * sprite.Width);
                texX = Math.Clamp(texX, 0, sprite.Width - 1);

                for (int screenY = Math.Max(0, drawTop); screenY < Math.Min(InternalHeight, drawBottom); screenY++)
                {
                    int texY = Math.Clamp((int)((screenY - drawTop) / (float)spriteHeight * sprite.Height), 0, sprite.Height - 1);
                    Color texel = sprite.Pixels[(texY * sprite.Width) + texX];
                    if (texel.A < 10) continue;

                    Color shaded = Modulate(texel, shade);
                    if (enemy.HitFlashAmount > 0f)
                    {
                        shaded = Raylib.ColorLerp(shaded, Color.Red, enemy.HitFlashAmount * 0.7f);
                    }

                    _framebuffer[(screenY * InternalWidth) + screenX] = shaded;
                }
            }
        }
    }

    private void DrawKeys(PlayerController player, int horizon)
    {
        foreach (KeyItem key in _map.Keys.Where(k => !k.IsCollected))
        {
            DrawBillboardSprite(key.Texture, key.Position, player, horizon, true, 0.25f);
        }
    }

    private void DrawBillboardSprite(Texture2D texture, Vector2 worldPos, PlayerController player, int horizon, bool checkDepth, float sizeScale = 1f)
    {
        float halfFov = _fov * 0.5f;
        Vector2 forward = new(MathF.Cos(player.Angle), MathF.Sin(player.Angle));
        Vector2 right = new(-forward.Y, forward.X);
        Vector2 plane = right * MathF.Tan(halfFov);
        float invDet = 1f / ((plane.X * forward.Y) - (forward.X * plane.Y));
        float projPlaneDist = (InternalWidth * 0.5f) / MathF.Tan(halfFov);
        var sprite = GetSpritePixels(texture);

        Vector2 rel = worldPos - player.Position;
        float transformX = invDet * ((forward.Y * rel.X) - (forward.X * rel.Y));
        float transformY = invDet * ((-plane.Y * rel.X) + (plane.X * rel.Y));
        if (transformY <= 0.001f) return;

        int spriteScreenX = (int)((InternalWidth * 0.5f) * (1f + (transformX / transformY)));
        int spriteHeight = Math.Max(1, (int)(DungeonMap.TileSize * projPlaneDist / transformY * sizeScale));
        int spriteWidth = spriteHeight;
        int drawBottom = horizon + Math.Max(1, spriteHeight);
        int drawTop = drawBottom - spriteHeight;
        int drawLeft = spriteScreenX - (spriteWidth / 2);
        int drawRight = drawLeft + spriteWidth;

        byte shade = ShadeByte(transformY, SpriteMinBrightness, 0.85f, AtmosphereDistanceScale * 0.9f);
        for (int screenX = Math.Max(0, drawLeft); screenX < Math.Min(InternalWidth, drawRight); screenX++)
        {
            if (checkDepth && transformY >= _depthBuffer[screenX]) continue;
            int texX = Math.Clamp((int)((screenX - drawLeft) / (float)spriteWidth * sprite.Width), 0, sprite.Width - 1);
            for (int screenY = Math.Max(0, drawTop); screenY < Math.Min(InternalHeight, drawBottom); screenY++)
            {
                int texY = Math.Clamp((int)((screenY - drawTop) / (float)spriteHeight * sprite.Height), 0, sprite.Height - 1);
                Color texel = sprite.Pixels[(texY * sprite.Width) + texX];
                if (texel.A < 10) continue;
                _framebuffer[(screenY * InternalWidth) + screenX] = Modulate(texel, shade);
            }
        }
    }

    private (Color[] Pixels, int Width, int Height) GetSpritePixels(Texture2D texture)
    {
        if (_spriteCache.TryGetValue(texture.Id, out var cached)) return cached;

        Image image = Raylib.LoadImageFromTexture(texture);
        Color[] colors = new Color[image.Width * image.Height];

        unsafe
        {
            Color* pixels = Raylib.LoadImageColors(image);
            for (int i = 0; i < colors.Length; i++) colors[i] = pixels[i];
            Raylib.UnloadImageColors(pixels);
        }

        Raylib.UnloadImage(image);

        cached = (colors, texture.Width, texture.Height);
        _spriteCache[texture.Id] = cached;
        return cached;
    }

    private List<RayHit> CastRay(Vector2 origin, float rayAngle)
    {
        List<RayHit> hits = [];
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

            DoorEntity? door = _map.GetDoorAtGrid(mapX, mapY);
            if (door is not null)
            {
                bool isOpenDoor = door.State == DoorState.Open;
                var doorTexture = GetSpritePixels(isOpenDoor ? _openDoorTexture : _closedDoorTexture);
                hits.Add(CreateRayHit(RayHitType.Door, origin, rayDir, distance, hitVertical, !isOpenDoor, doorTexture));
                if (!isOpenDoor) break;
            }

            if (_map.IsWallAtGrid(mapX, mapY))
            {
                var wallTexture = (_wallPixels, _wallWidth, _wallHeight);
                hits.Add(CreateRayHit(RayHitType.Wall, origin, rayDir, distance, hitVertical, true, wallTexture));
                break;
            }
        }

        return hits;
    }

    private static RayHit CreateRayHit(RayHitType type, Vector2 origin, Vector2 rayDir, float distance, bool hitVertical, bool blocksView, (Color[] Pixels, int Width, int Height) texture)
    {
        Vector2 hitPoint = origin + rayDir * distance;
        float textureCoord = hitVertical ? hitPoint.Y : hitPoint.X;
        float textureX = textureCoord % DungeonMap.TileSize;
        if (textureX < 0f) textureX += DungeonMap.TileSize;
        return new RayHit(type, distance, (textureX / DungeonMap.TileSize) * texture.Width, hitVertical, blocksView, texture);
    }

    private static Color Modulate(Color c, byte shade)
        => new((byte)(c.R * shade / 255), (byte)(c.G * shade / 255), (byte)(c.B * shade / 255), (byte)255);

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

    private static float DistanceShade(float distance, float min, float max, float falloff)
        => Math.Clamp(1f - (distance / MathF.Max(1f, falloff)), min, max);

    private static byte ShadeByte(float distance, float min, float max, float falloff)
        => (byte)(DistanceShade(distance, min, max, falloff) * 255f);

    private static Vector2 WorldToMinimap(Vector2 worldPos, Vector2 playerPos, Vector2 minimapCenter, float pixelsPerWorldUnit)
        => minimapCenter + ((worldPos - playerPos) * pixelsPerWorldUnit);

    private static bool IsMinimapPointVisible(Vector2 point, Rectangle bounds, float padding = 0f)
        => point.X >= bounds.X - padding
        && point.X <= bounds.X + bounds.Width + padding
        && point.Y >= bounds.Y - padding
        && point.Y <= bounds.Y + bounds.Height + padding;

    private static Rectangle ClipRect(Rectangle rect, Rectangle bounds)
    {
        float left = MathF.Max(rect.X, bounds.X);
        float top = MathF.Max(rect.Y, bounds.Y);
        float right = MathF.Min(rect.X + rect.Width, bounds.X + bounds.Width);
        float bottom = MathF.Min(rect.Y + rect.Height, bounds.Y + bounds.Height);
        return right <= left || bottom <= top ? new Rectangle(0, 0, 0, 0) : new Rectangle(left, top, right - left, bottom - top);
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
        const int visibleTiles = 9;
        const int cell = 28;
        const int offsetX = 16;
        const int offsetY = 16;
        int mapPixelWidth = visibleTiles * cell;
        int mapPixelHeight = visibleTiles * cell;
        int borderPadding = 10;
        int playerTileX = (int)(player.Position.X / DungeonMap.TileSize);
        int playerTileY = (int)(player.Position.Y / DungeonMap.TileSize);
        int halfTiles = visibleTiles / 2;
        float pixelsPerWorldUnit = cell / (float)DungeonMap.TileSize;
        Rectangle minimapBounds = new(offsetX, offsetY, mapPixelWidth, mapPixelHeight);
        Vector2 minimapCenter = new(offsetX + (mapPixelWidth * 0.5f), offsetY + (mapPixelHeight * 0.5f));

        Raylib.DrawRectangle(offsetX - 6, offsetY - 6, mapPixelWidth + 12, mapPixelHeight + 12, new Color(10, 14, 20, 200));
        Raylib.DrawRectangleLines(offsetX - 6, offsetY - 6, mapPixelWidth + 12, mapPixelHeight + 12, new Color(120, 128, 144, 220));

        int searchRadius = halfTiles + 2;
        for (int mapY = playerTileY - searchRadius; mapY <= playerTileY + searchRadius; mapY++)
        {
            for (int mapX = playerTileX - searchRadius; mapX <= playerTileX + searchRadius; mapX++)
            {
                if (mapX < 0 || mapX >= _map.Width || mapY < 0 || mapY >= _map.Height) continue;

                Vector2 tileCenterWorld = new((mapX + 0.5f) * DungeonMap.TileSize, (mapY + 0.5f) * DungeonMap.TileSize);
                Vector2 tileCenter = WorldToMinimap(tileCenterWorld, player.Position, minimapCenter, pixelsPerWorldUnit);
                if (!IsMinimapPointVisible(tileCenter, minimapBounds, cell * 0.75f)) continue;

                float viewX = (tileCenter.X - offsetX) / cell;
                float viewY = (tileCenter.Y - offsetY) / cell;
                float normalizedEdgeDist = MathF.Max(MathF.Abs(viewX - halfTiles), MathF.Abs(viewY - halfTiles)) / halfTiles;
                float edgeFade = Math.Clamp(1f - normalizedEdgeDist * 0.45f, 0.45f, 1f);

                bool wall = _map.IsWallAtGrid(mapX, mapY);
                Color baseColor = wall ? new Color(84, 86, 90, 220) : new Color(32, 36, 40, 170);
                Color faded = Modulate(baseColor, (byte)(edgeFade * 255));

                Rectangle tileRect = new(tileCenter.X - (cell * 0.5f), tileCenter.Y - (cell * 0.5f), cell, cell);
                tileRect = ClipRect(tileRect, minimapBounds);
                if (tileRect.Width <= 0 || tileRect.Height <= 0) continue;
                Raylib.DrawRectangleRec(tileRect, faded);
            }
        }

        foreach (Enemy enemy in _map.Enemies.Where(e => e.IsAlive))
        {
            Vector2 pos = WorldToMinimap(enemy.Position, player.Position, minimapCenter, pixelsPerWorldUnit);
            if (!IsMinimapPointVisible(pos, minimapBounds, 3f)) continue;
            Raylib.DrawCircle((int)pos.X, (int)pos.Y, 3f, Color.Red);
        }

        foreach (KeyItem key in _map.Keys.Where(k => !k.IsCollected))
        {
            Vector2 pos = WorldToMinimap(key.Position, player.Position, minimapCenter, pixelsPerWorldUnit);
            if (!IsMinimapPointVisible(pos, minimapBounds, 8f)) continue;
            const int keyIconSize = 12;
            Rectangle dst = new(pos.X - (keyIconSize / 2), pos.Y - (keyIconSize / 2), keyIconSize, keyIconSize);
            Raylib.DrawTexturePro(_minimapKeyTexture, new Rectangle(0, 0, _minimapKeyTexture.Width, _minimapKeyTexture.Height), dst, Vector2.Zero, 0f, Color.Gold);
        }

        foreach (DoorEntity door in _map.Doors)
        {
            Vector2 pos = WorldToMinimap(door.Position, player.Position, minimapCenter, pixelsPerWorldUnit);
            if (!IsMinimapPointVisible(pos, minimapBounds, 10f)) continue;
            DrawDoorMarker(door, player, pos);
        }

        Vector2 playerMinimapPos = WorldToMinimap(player.Position, player.Position, minimapCenter, pixelsPerWorldUnit);
        float px = Math.Clamp(playerMinimapPos.X, offsetX + borderPadding, offsetX + mapPixelWidth - borderPadding);
        float py = Math.Clamp(playerMinimapPos.Y, offsetY + borderPadding, offsetY + mapPixelHeight - borderPadding);

        Vector2 forward = new(MathF.Cos(player.Angle), MathF.Sin(player.Angle));
        Vector2 right = new(-forward.Y, forward.X);
        Vector2 center = new(px, py);
        Vector2 tip = center + (forward * 8f);
        Vector2 left = center - (forward * 4f) - (right * 4f);
        Vector2 rightPoint = center - (forward * 4f) + (right * 4f);

        Raylib.DrawTriangle(tip, left, rightPoint, new Color(64, 196, 255, 255));
    }

    private void DrawDoorMarker(DoorEntity door, PlayerController player, Vector2 minimapPosition)
    {
        bool hasNoRequiredKey = string.IsNullOrWhiteSpace(door.RequiredKeyId);
        bool hasMatchingKey = player.HasKey(door.RequiredKeyId);

        if (hasNoRequiredKey || hasMatchingKey)
        {
            DrawUnlockedDoorMarker(minimapPosition);
            return;
        }

        DrawLockedDoorMarker(minimapPosition);
    }

    private void DrawLockedDoorMarker(Vector2 minimapPosition)
        => DrawLockIcon(minimapPosition);

    private void DrawUnlockedDoorMarker(Vector2 minimapPosition)
    {
        DrawLockIcon(minimapPosition);
        DrawTickOverlay(minimapPosition);
    }

    private void DrawTickOverlay(Vector2 minimapPosition)
    {
        const int iconSize = 16;
        Rectangle dst = new(minimapPosition.X - (iconSize / 2), minimapPosition.Y - (iconSize / 2), iconSize, iconSize);
        Raylib.DrawTexturePro(_minimapTickTexture, new Rectangle(0, 0, _minimapTickTexture.Width, _minimapTickTexture.Height), dst, Vector2.Zero, 0f, Color.White);
    }

    private void DrawLockIcon(Vector2 minimapPosition)
    {
        const int iconSize = 16;
        Rectangle dst = new(minimapPosition.X - (iconSize / 2), minimapPosition.Y - (iconSize / 2), iconSize, iconSize);
        Raylib.DrawTexturePro(_minimapLockTexture, new Rectangle(0, 0, _minimapLockTexture.Width, _minimapLockTexture.Height), dst, Vector2.Zero, 0f, Color.White);
    }

}
