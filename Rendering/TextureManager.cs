using Raylib_cs;

namespace DungeonCrawler.Rendering;

public sealed class TextureManager : IDisposable
{
    public Texture2D DungeonTexture { get; }
    public Texture2D PlayerAnimationsTexture { get; }
    public Texture2D GoblinTexture { get; }
    public Texture2D GoblinLightWindupTexture { get; }
    public Texture2D GoblinHeavyWindupTexture { get; }
    public Texture2D GoblinStrikeTexture { get; }
    public Texture2D GoblinStaggerTexture { get; }
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
        GoblinLightWindupTexture = Raylib.LoadTexture("Assets/Textures/goblin_light_windup_strike.png");
        GoblinHeavyWindupTexture = Raylib.LoadTexture("Assets/Textures/goblin_heavy_windup_strike.png");
        GoblinStrikeTexture = Raylib.LoadTexture("Assets/Textures/goblin_strike.png");
        GoblinStaggerTexture = Raylib.LoadTexture("Assets/Textures/goblin_stagger.png");
        Raylib.SetTextureFilter(GoblinTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(GoblinLightWindupTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(GoblinHeavyWindupTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(GoblinStrikeTexture, TextureFilter.Point);
        Raylib.SetTextureFilter(GoblinStaggerTexture, TextureFilter.Point);

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
        Raylib.UnloadTexture(GoblinLightWindupTexture);
        Raylib.UnloadTexture(GoblinHeavyWindupTexture);
        Raylib.UnloadTexture(GoblinStrikeTexture);
        Raylib.UnloadTexture(GoblinStaggerTexture);
        Raylib.UnloadTexture(KeyTexture);
        Raylib.UnloadTexture(ClosedDoorTexture);
        Raylib.UnloadTexture(OpenDoorTexture);
        Raylib.UnloadTexture(MinimapLockTexture);
        Raylib.UnloadTexture(MinimapTickTexture);
    }
}
