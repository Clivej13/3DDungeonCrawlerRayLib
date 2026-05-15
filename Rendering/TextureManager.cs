using Raylib_cs;

namespace DungeonCrawler.Rendering;

public sealed class TextureManager : IDisposable
{
    public Texture2D DungeonTexture { get; }

    public TextureManager()
    {
        DungeonTexture = Raylib.LoadTexture("Assets/Textures/repeatable_grey_brick.png");
        Raylib.SetTextureFilter(DungeonTexture, TextureFilter.Point);
        Raylib.SetTextureWrap(DungeonTexture, TextureWrap.Repeat);
    }

    public void Dispose()
    {
        Raylib.UnloadTexture(DungeonTexture);
    }
}
