using Raylib_cs;

namespace DungeonCrawler.Rendering;

public sealed class TextureManager : IDisposable
{
    public Texture2D DungeonTexture { get; }
    public Texture2D PlayerAnimationsTexture { get; }
    public Texture2D GoblinTexture { get; }

    public TextureManager()
    {
        DungeonTexture = Raylib.LoadTexture("Assets/Textures/repeatable_grey_brick.png");
        Raylib.SetTextureFilter(DungeonTexture, TextureFilter.Point);
        Raylib.SetTextureWrap(DungeonTexture, TextureWrap.Repeat);

        PlayerAnimationsTexture = Raylib.LoadTexture("Assets/Textures/player_animations.png");
        Raylib.SetTextureFilter(PlayerAnimationsTexture, TextureFilter.Point);

        GoblinTexture = Raylib.LoadTexture("Assets/Textures/goblin.png");
        Raylib.SetTextureFilter(GoblinTexture, TextureFilter.Point);
    }

    public void Dispose()
    {
        Raylib.UnloadTexture(DungeonTexture);
        Raylib.UnloadTexture(PlayerAnimationsTexture);
        Raylib.UnloadTexture(GoblinTexture);
    }
}
