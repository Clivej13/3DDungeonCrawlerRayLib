using Raylib_cs;

namespace DungeonCrawler.Rendering;

public sealed class TextureManager : IDisposable
{
    public Texture2D DungeonTexture { get; }
    public Texture2D PlayerAnimationsTexture { get; }

    public TextureManager()
    {
        DungeonTexture = Raylib.LoadTexture("Assets/Textures/repeatable_grey_brick.png");
        Raylib.SetTextureFilter(DungeonTexture, TextureFilter.Point);
        Raylib.SetTextureWrap(DungeonTexture, TextureWrap.Repeat);

        PlayerAnimationsTexture = Raylib.LoadTexture("Assets/Textures/player_animations.png");
        Raylib.SetTextureFilter(PlayerAnimationsTexture, TextureFilter.Point);
    }

    public void Dispose()
    {
        Raylib.UnloadTexture(DungeonTexture);
        Raylib.UnloadTexture(PlayerAnimationsTexture);
    }
}
