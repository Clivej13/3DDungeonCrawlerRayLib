using Raylib_cs;

namespace DungeonCrawler.Rendering;

public sealed class TextureManager : IDisposable
{
    public Texture2D DungeonTexture { get; }
    public Texture2D PlayerAnimationsTexture { get; }
    public Texture2D GoblinTexture { get; }
    public Texture2D SilverKeyTexture { get; }
    public Texture2D GoldKeyTexture { get; }
    public Texture2D ClosedDoorTexture { get; }
    public Texture2D OpenDoorTexture { get; }
    public Texture2D SilverLockTexture { get; }
    public Texture2D GoldLockTexture { get; }
    public Texture2D TickLockTexture { get; }

    public TextureManager()
    {
        DungeonTexture = Raylib.LoadTexture("Assets/Textures/repeatable_grey_brick.png");
        Raylib.SetTextureFilter(DungeonTexture, TextureFilter.Point);
        Raylib.SetTextureWrap(DungeonTexture, TextureWrap.Repeat);

        PlayerAnimationsTexture = Raylib.LoadTexture("Assets/Textures/player_animations.png");
        Raylib.SetTextureFilter(PlayerAnimationsTexture, TextureFilter.Point);

        GoblinTexture = Raylib.LoadTexture("Assets/Textures/goblin.png");
        Raylib.SetTextureFilter(GoblinTexture, TextureFilter.Point);

        SilverKeyTexture = Raylib.LoadTexture("Assets/Textures/silver_key.png");
        GoldKeyTexture = Raylib.LoadTexture("Assets/Textures/gold_key.png");
        ClosedDoorTexture = Raylib.LoadTexture("Assets/Textures/closed_door.png");
        OpenDoorTexture = Raylib.LoadTexture("Assets/Textures/open_door.png");
        SilverLockTexture = Raylib.LoadTexture("Assets/Textures/silver_lock.png");
        GoldLockTexture = Raylib.LoadTexture("Assets/Textures/gold_lock.png");
        TickLockTexture = Raylib.LoadTexture("Assets/Textures/tick_lock.png");
        Raylib.SetTextureFilter(SilverKeyTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(GoldKeyTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(ClosedDoorTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(OpenDoorTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(SilverLockTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(GoldLockTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(TickLockTexture, TextureFilter.Point);
    }

    public void Dispose()
    {
        Raylib.UnloadTexture(DungeonTexture);
        Raylib.UnloadTexture(PlayerAnimationsTexture);
        Raylib.UnloadTexture(GoblinTexture);
        Raylib.UnloadTexture(SilverKeyTexture);
        Raylib.UnloadTexture(GoldKeyTexture);
        Raylib.UnloadTexture(ClosedDoorTexture);
        Raylib.UnloadTexture(OpenDoorTexture);
        Raylib.UnloadTexture(SilverLockTexture);
        Raylib.UnloadTexture(GoldLockTexture);
        Raylib.UnloadTexture(TickLockTexture);
    }
}
