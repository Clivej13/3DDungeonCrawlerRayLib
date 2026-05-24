using Raylib_cs;

namespace DungeonCrawler.Rendering;

public sealed class TextureManager : IDisposable
{
    public Texture2D DungeonTexture { get; }
    public Texture2D PlayerAnimationsTexture { get; }
    public Texture2D GoblinTexture { get; }
    public Texture2D GoblinWindupTexture { get; }
    public Texture2D GoblinAttackTexture { get; }
    public Texture2D KeyTexture { get; }
    public Texture2D ClosedDoorTexture { get; }
    public Texture2D OpenDoorTexture { get; }
    public Texture2D MinimapLockTexture { get; }
    public Texture2D MinimapTickTexture { get; }

    public TextureManager()
    {
        DungeonTexture = Raylib.LoadTexture("Assets/Textures/repeatable_grey_brick.png");
        Raylib.SetTextureFilter(DungeonTexture, TextureFilter.Point);
        Raylib.SetTextureWrap(DungeonTexture, TextureWrap.Repeat);

        PlayerAnimationsTexture = Raylib.LoadTexture("Assets/Textures/player_animations.png");
        Raylib.SetTextureFilter(PlayerAnimationsTexture, TextureFilter.Point);

        GoblinTexture = Raylib.LoadTexture("Assets/Textures/goblin.png");
        GoblinWindupTexture = Raylib.LoadTexture("Assets/Textures/goblin_strike_windup_dodge.png");
        GoblinAttackTexture = Raylib.LoadTexture("Assets/Textures/goblin_strike_dodge.png");
        Raylib.SetTextureFilter(GoblinTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(GoblinWindupTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(GoblinAttackTexture, TextureFilter.Point);

        KeyTexture = Raylib.LoadTexture("Assets/Textures/gold_key.png");
        ClosedDoorTexture = Raylib.LoadTexture("Assets/Textures/closed_door.png");
        OpenDoorTexture = Raylib.LoadTexture("Assets/Textures/open_door.png");
        MinimapLockTexture = Raylib.LoadTexture("Assets/Textures/silver_lock.png");
        MinimapTickTexture = Raylib.LoadTexture("Assets/Textures/tick_lock.png");
        Raylib.SetTextureFilter(KeyTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(ClosedDoorTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(OpenDoorTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(MinimapLockTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(MinimapTickTexture, TextureFilter.Point);
    }

    public void Dispose()
    {
        Raylib.UnloadTexture(DungeonTexture);
        Raylib.UnloadTexture(PlayerAnimationsTexture);
        Raylib.UnloadTexture(GoblinTexture);
        Raylib.UnloadTexture(GoblinWindupTexture);
        Raylib.UnloadTexture(GoblinAttackTexture);
        Raylib.UnloadTexture(KeyTexture);
        Raylib.UnloadTexture(ClosedDoorTexture);
        Raylib.UnloadTexture(OpenDoorTexture);
        Raylib.UnloadTexture(MinimapLockTexture);
        Raylib.UnloadTexture(MinimapTickTexture);
    }
}
