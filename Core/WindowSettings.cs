using Raylib_cs;

namespace DungeonCrawler.Core;

/// <summary>
/// Runtime window/display settings. Shared by all menus so settings are reusable.
/// </summary>
public sealed class WindowSettings
{
    public readonly (int Width, int Height)[] Resolutions =
    {
        (1280, 720),
        (1600, 900),
        (1920, 1080)
    };

    public int ResolutionIndex { get; private set; } = 0;
    public bool IsFullscreen { get; private set; } = false;

    public (int Width, int Height) CurrentResolution => Resolutions[ResolutionIndex];

    public void SetResolutionIndex(int index)
    {
        if (index < 0 || index >= Resolutions.Length)
        {
            return;
        }

        ResolutionIndex = index;
        var (width, height) = CurrentResolution;
        Raylib.SetWindowSize(width, height);
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
    }
}
