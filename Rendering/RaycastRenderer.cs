using DungeonCrawler.Entities;
using DungeonCrawler.Player;
using DungeonCrawler.World;
using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Rendering;

public sealed class RaycastRenderer
{
    private readonly DungeonMap _map;
    private readonly Texture2D _wallTexture;
    private readonly float[] _depthBuffer;
    private readonly Dictionary<uint, (Color[] Pixels, int Width, int Height)> _spriteCache = [];

    private readonly float _fov = MathF.PI / 3.2f;
    private readonly float _maxRayDistance = 1200f;

    private const int InternalWidth = 320;
    private const int InternalHeight = 200;

    private const float AtmosphereDistanceScale = 620f;
    private const float WallMinBrightness = 0.08f;
    private const float DoorMinBrightness = 0.08f;
    private const float SpriteMinBrightness = 0.10f;

    private readonly Color[] _framebuffer;
    private readonly Texture2D _frameTexture;
    private readonly Color[] _wallPixels;
    private readonly int _wallWidth;
    private readonly int _wallHeight;
    private readonly Texture2D _closedDoorTexture;
    private readonly Texture2D _openDoorTexture;
    private readonly Texture2D _silverLockTexture;
    private readonly Texture2D _goldLockTexture;
    private readonly Texture2D _tickLockTexture;

    public RaycastRenderer(DungeonMap map, Texture2D wallTexture, Texture2D closedDoorTexture, Texture2D openDoorTexture, Texture2D silverLockTexture, Texture2D goldLockTexture, Texture2D tickLockTexture)
    {
        _map = map;
        _wallTexture = wallTexture;
        _closedDoorTexture = closedDoorTexture;
        _openDoorTexture = openDoorTexture;
        _silverLockTexture = silverLockTexture;
        _goldLockTexture = goldLockTexture;
        _tickLockTexture = tickLockTexture;

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
        DrawWalls(player, horizon);
        DrawDoors(player, horizon);
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

        float shadeMin = isFloor ? 0.08f : 0.03f;
        float shadeMax = isFloor ? 0.46f : 0.18f;
        float falloff = isFloor ? AtmosphereDistanceScale * 0.5f : AtmosphereDistanceScale * 0.35f;

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
            if (isFloor) distanceShade *= 0.78f;
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
            _depthBuffer[x] = correctedDist;

            int sliceHeight = (int)((DungeonMap.TileSize / correctedDist) * projPlaneDist);
            int drawTop = horizon - (sliceHeight / 2);
            int drawBottom = drawTop + sliceHeight;

            byte shade = ShadeByte(correctedDist, WallMinBrightness, 0.96f, AtmosphereDistanceScale);
            if (hit.HitVertical) shade = (byte)(shade * 0.70f);

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

    private void DrawEnemies(PlayerController player, int horizon)
    {
        float halfFov = _fov * 0.5f;
        float invDet; // camera inverse determinant for world->camera transform

        Vector2 forward = new(MathF.Cos(player.Angle), MathF.Sin(player.Angle));
        Vector2 right = new(-forward.Y, forward.X);

        // camera plane magnitude equals tan(halfFov)
        Vector2 plane = right * MathF.Tan(halfFov);
        invDet = 1f / ((plane.X * forward.Y) - (forward.X * plane.Y));

        float projPlaneDist = (InternalWidth * 0.5f) / MathF.Tan(halfFov);

        foreach (Enemy enemy in _map.Enemies.Where(e => e.IsAlive).OrderByDescending(e => e.DistanceToPlayer))
        {
            Vector2 rel = enemy.Position - player.Position;

            float transformX = invDet * ((forward.Y * rel.X) - (forward.X * rel.Y));
            float transformY = invDet * ((-plane.Y * rel.X) + (plane.X * rel.Y)); // perpendicular depth

            if (transformY <= 0.001f)
            {
                continue; // behind player
            }

            float angleToEnemy = MathF.Atan2(rel.Y, rel.X) - player.Angle;
            angleToEnemy = MathF.Atan2(MathF.Sin(angleToEnemy), MathF.Cos(angleToEnemy));
            if (MathF.Abs(angleToEnemy) > halfFov)
            {
                continue; // outside FOV
            }

            int spriteScreenX = (int)((InternalWidth * 0.5f) * (1f + (transformX / transformY)));
            int spriteHeight = Math.Max(1, (int)(DungeonMap.TileSize * projPlaneDist / transformY * 0.50f));
            int spriteWidth = spriteHeight;

            int drawBottom = horizon + spriteHeight; // floor-aligned anchor so feet sit on floor
            int drawTop = drawBottom - spriteHeight;
            int drawLeft = spriteScreenX - (spriteWidth / 2);
            int drawRight = drawLeft + spriteWidth;

            var sprite = GetSpritePixels(enemy.Texture);
            byte shade = ShadeByte(transformY, SpriteMinBrightness, 0.92f, AtmosphereDistanceScale);

            for (int screenX = Math.Max(0, drawLeft); screenX < Math.Min(InternalWidth, drawRight); screenX++)
            {
                if (transformY >= _depthBuffer[screenX])
                {
                    continue;
                }

                int texX = (int)((screenX - drawLeft) / (float)spriteWidth * sprite.Width);
                texX = Math.Clamp(texX, 0, sprite.Width - 1);

                for (int screenY = Math.Max(0, drawTop); screenY < Math.Min(InternalHeight, drawBottom); screenY++)
                {
                    int texY = (int)((screenY - drawTop) / (float)spriteHeight * sprite.Height);
                    texY = Math.Clamp(texY, 0, sprite.Height - 1);

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

    private void DrawDoors(PlayerController player, int horizon)
    {
        float halfFov = _fov * 0.5f;
        float projPlaneDist = (InternalWidth * 0.5f) / MathF.Tan(halfFov);

        for (int x = 0; x < InternalWidth; x++)
        {
            float cameraX = (2f * x / InternalWidth) - 1f;
            float rayAngle = player.Angle + cameraX * halfFov;
            var hit = CastRay(player.Position, rayAngle, includeDoors: true);
            if (!hit.HitDoor) continue;

            float correctedDist = MathF.Max(hit.Distance * MathF.Cos(rayAngle - player.Angle), 0.0001f);
            if (correctedDist >= _depthBuffer[x]) continue;
            _depthBuffer[x] = correctedDist;

            int sliceHeight = (int)((DungeonMap.TileSize / correctedDist) * projPlaneDist);
            int drawTop = horizon - (sliceHeight / 2);
            int drawBottom = drawTop + sliceHeight;
            var doorSprite = GetSpritePixels(hit.IsLockedDoor ? _closedDoorTexture : _openDoorTexture);
            int texX = Math.Clamp((int)hit.TextureX, 0, doorSprite.Width - 1);
            byte shade = ShadeByte(correctedDist, DoorMinBrightness, 0.92f, AtmosphereDistanceScale);

            for (int y = Math.Max(0, drawTop); y < Math.Min(InternalHeight, drawBottom); y++)
            {
                float t = (y - drawTop) / (float)Math.Max(sliceHeight, 1);
                int texY = Math.Clamp((int)(t * doorSprite.Height), 0, doorSprite.Height - 1);
                Color c = doorSprite.Pixels[(texY * doorSprite.Width) + texX];
                if (c.A < 10) continue;
                _framebuffer[y * InternalWidth + x] = Modulate(c, shade);
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

        byte shade = ShadeByte(transformY, SpriteMinBrightness, 0.92f, AtmosphereDistanceScale);
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

    private (float Distance, float TextureX, bool HitVertical, bool HitDoor, bool IsLockedDoor) CastRay(Vector2 origin, float rayAngle, bool includeDoors = false)
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
        bool hitDoor = false;
        bool isLockedDoor = false;

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
            if (includeDoors)
            {
                DoorEntity? door = _map.Doors.FirstOrDefault(d => (int)(d.Position.X / DungeonMap.TileSize) == mapX && (int)(d.Position.Y / DungeonMap.TileSize) == mapY);
                if (door is not null)
                {
                    hitDoor = true;
                    isLockedDoor = door.IsLocked;
                    break;
                }
            }
        }

        Vector2 hitPoint = origin + rayDir * distance;
        float textureCoord = hitVertical ? hitPoint.Y : hitPoint.X;
        float textureX = textureCoord % DungeonMap.TileSize;
        if (textureX < 0f) textureX += DungeonMap.TileSize;

        return (distance, (textureX / DungeonMap.TileSize) * _wallWidth, hitVertical, hitDoor, isLockedDoor);
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

    private static bool TryGetMinimapPoint(Vector2 worldPos, Vector2 playerPos, int visibleTiles, int offsetX, int offsetY, int cell, out Vector2 minimapPos)
    {
        float deltaTilesX = (worldPos.X - playerPos.X) / DungeonMap.TileSize;
        float deltaTilesY = (worldPos.Y - playerPos.Y) / DungeonMap.TileSize;
        float halfTiles = visibleTiles * 0.5f;

        if (MathF.Abs(deltaTilesX) > halfTiles || MathF.Abs(deltaTilesY) > halfTiles)
        {
            minimapPos = default;
            return false;
        }

        float centerX = offsetX + (visibleTiles * cell * 0.5f);
        float centerY = offsetY + (visibleTiles * cell * 0.5f);
        minimapPos = new Vector2(centerX + (deltaTilesX * cell), centerY + (deltaTilesY * cell));
        return true;
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

        Raylib.DrawRectangle(offsetX - 6, offsetY - 6, mapPixelWidth + 12, mapPixelHeight + 12, new Color(10, 14, 20, 200));
        Raylib.DrawRectangleLines(offsetX - 6, offsetY - 6, mapPixelWidth + 12, mapPixelHeight + 12, new Color(120, 128, 144, 220));

        for (int viewY = 0; viewY < visibleTiles; viewY++)
        {
            for (int viewX = 0; viewX < visibleTiles; viewX++)
            {
                int mapX = (playerTileX - halfTiles) + viewX;
                int mapY = (playerTileY - halfTiles) + viewY;
                bool inBounds = mapX >= 0 && mapX < _map.Width && mapY >= 0 && mapY < _map.Height;
                bool wall = inBounds && _map.IsWallAtGrid(mapX, mapY);
                float normalizedEdgeDist = MathF.Max(MathF.Abs(viewX - halfTiles), MathF.Abs(viewY - halfTiles)) / halfTiles;
                float edgeFade = Math.Clamp(1f - normalizedEdgeDist * 0.45f, 0.45f, 1f);

                Color baseColor = wall ? new Color(84, 86, 90, 220) : new Color(32, 36, 40, 170);
                Color faded = Modulate(baseColor, (byte)(edgeFade * 255));
                Raylib.DrawRectangle(offsetX + viewX * cell, offsetY + viewY * cell, cell - 1, cell - 1, faded);
            }
        }

        foreach (Enemy enemy in _map.Enemies.Where(e => e.IsAlive))
        {
            if (!TryGetMinimapPoint(enemy.Position, player.Position, visibleTiles, offsetX, offsetY, cell, out Vector2 pos)) continue;
            Raylib.DrawCircle((int)pos.X, (int)pos.Y, 3f, Color.Red);
        }

        foreach (KeyItem key in _map.Keys.Where(k => !k.IsCollected))
        {
            if (!TryGetMinimapPoint(key.Position, player.Position, visibleTiles, offsetX, offsetY, cell, out Vector2 pos)) continue;
            Texture2D keyIcon = key.Type == KeyType.Silver ? GetMinimapKeyTexture(KeyType.Silver) : GetMinimapKeyTexture(KeyType.Gold);
            const int keyIconSize = 12;
            Rectangle dst = new(pos.X - (keyIconSize / 2), pos.Y - (keyIconSize / 2), keyIconSize, keyIconSize);
            Raylib.DrawTexturePro(keyIcon, new Rectangle(0, 0, keyIcon.Width, keyIcon.Height), dst, Vector2.Zero, 0f, Color.White);
        }

        foreach (DoorEntity door in _map.Doors)
        {
            if (!door.IsLocked) continue; // opened doors render nothing on minimap
            if (!TryGetMinimapPoint(door.Position, player.Position, visibleTiles, offsetX, offsetY, cell, out Vector2 pos)) continue;

            Texture2D lockIcon = door.Type == KeyType.Silver ? _silverLockTexture : _goldLockTexture;
            Rectangle dst = new(pos.X - 8, pos.Y - 8, 16, 16);
            Raylib.DrawTexturePro(lockIcon, new Rectangle(0, 0, lockIcon.Width, lockIcon.Height), dst, Vector2.Zero, 0f, Color.White);

            bool hasRequiredKey = door.Type == KeyType.Silver ? player.HasSilverKey : player.HasGoldKey;
            if (hasRequiredKey)
            {
                Raylib.DrawTexturePro(_tickLockTexture, new Rectangle(0, 0, _tickLockTexture.Width, _tickLockTexture.Height), dst, Vector2.Zero, 0f, Color.White);
            }
        }

        float px = offsetX + (mapPixelWidth * 0.5f);
        float py = offsetY + (mapPixelHeight * 0.5f);
        px = Math.Clamp(px, offsetX + borderPadding, offsetX + mapPixelWidth - borderPadding);
        py = Math.Clamp(py, offsetY + borderPadding, offsetY + mapPixelHeight - borderPadding);

        Vector2 forward = new(MathF.Cos(player.Angle), MathF.Sin(player.Angle));
        Vector2 right = new(-forward.Y, forward.X);
        Vector2 center = new(px, py);
        Vector2 tip = center + (forward * 8f);
        Vector2 left = center - (forward * 4f) - (right * 4f);
        Vector2 rightPoint = center - (forward * 4f) + (right * 4f);

        Raylib.DrawTriangle(tip, left, rightPoint, new Color(64, 196, 255, 255));
    }

    private Texture2D GetMinimapKeyTexture(KeyType keyType)
    {
        KeyItem? key = _map.Keys.FirstOrDefault(k => k.Type == keyType);
        return key?.Texture ?? (keyType == KeyType.Silver ? _silverLockTexture : _goldLockTexture);
    }
}
