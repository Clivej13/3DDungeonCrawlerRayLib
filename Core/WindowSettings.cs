using Raylib_cs;

namespace DungeonCrawler.Core;

/// <summary>
/// Runtime window/display settings. Shared by all menus so settings are reusable.
/// </summary>
public sealed class WindowSettings
{
    private readonly GameOptions _options;
    private readonly Action _saveOptions;

    public readonly (int Width, int Height)[] Resolutions =
    {
        (1280, 720),
        (1600, 900),
        (1920, 1080)
    };

    public int ResolutionIndex
    {
        get => _options.ResolutionIndex;
        private set => _options.ResolutionIndex = value;
    }

    public bool IsFullscreen
    {
        get => _options.IsFullscreen;
        private set => _options.IsFullscreen = value;
    }

    public (int Width, int Height) CurrentResolution => Resolutions[ResolutionIndex];

    public WindowSettings(GameOptions options, Action saveOptions)
    {
        _options = options;
        _saveOptions = saveOptions;

        if (ResolutionIndex < 0 || ResolutionIndex >= Resolutions.Length)
        {
            ResolutionIndex = 0;
        }
    }

    public void SetResolutionIndex(int index)
    {
        if (index < 0 || index >= Resolutions.Length)
        {
            return;
        }

        ResolutionIndex = index;
        var (width, height) = CurrentResolution;
        Raylib.SetWindowSize(width, height);
        _saveOptions();
    }

    public void CycleResolution(int direction)
    {
        var newIndex = ResolutionIndex + direction;
        if (newIndex < 0)
        {
            newIndex = Resolutions.Length - 1;
        }
        else if (newIndex >= Resolutions.Length)
        {
            newIndex = 0;
        }

        SetResolutionIndex(newIndex);
    }

    public void ToggleFullscreen()
    {
        Raylib.ToggleFullscreen();
        IsFullscreen = !IsFullscreen;
        _saveOptions();
    }
}
