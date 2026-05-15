using Raylib_cs;
using System.Numerics;

namespace DungeonCrawler.Rendering;

public sealed class WeaponRenderer
{
    private readonly Texture2D _spriteSheet;

    private const int CellSize = 64;
    private const int AnimationFrameCount = 7;
    private const float AnimationFrameDuration = 0.075f;

    private int _currentFrame;
    private float _frameTimer;
    private bool _animationPlaying;

    public bool IsSwinging => _animationPlaying;
    public int CurrentFrame => _currentFrame;

    public WeaponRenderer(Texture2D spriteSheet)
    {
        _spriteSheet = spriteSheet;
    }

    public void TriggerAttack()
    {
        if (_animationPlaying) return;

        _animationPlaying = true;
        _currentFrame = 0;
        _frameTimer = 0f;
    }

    public void Update(float dt)
    {
        if (!_animationPlaying) return;

        _frameTimer += dt;

        while (_frameTimer >= AnimationFrameDuration && _animationPlaying)
        {
            _frameTimer -= AnimationFrameDuration;
            _currentFrame++;

            if (_currentFrame >= AnimationFrameCount)
            {
                _currentFrame = 0;
                _animationPlaying = false;
            }
        }
    }

    public void Draw()
    {
        int screenW = Raylib.GetScreenWidth();
        int screenH = Raylib.GetScreenHeight();

        Rectangle source = new(_currentFrame * CellSize, 0, CellSize, CellSize);

        float scale = MathF.Max(3.0f, MathF.Min(screenW / 320f, screenH / 200f) * 2.5f);
        float drawW = CellSize * scale;
        float drawH = CellSize * scale;

        float marginX = 8f * MathF.Max(1f, screenW / 320f);
        float marginY = 6f * MathF.Max(1f, screenH / 200f);

        Rectangle dest = new(screenW - drawW - marginX, screenH - drawH - marginY, drawW, drawH);

        Raylib.DrawTexturePro(_spriteSheet, source, dest, Vector2.Zero, 0f, Color.White);
    }
}
