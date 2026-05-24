using System.Text.Json;

namespace DungeonCrawler.Core;

public sealed class GameOptions
{
    private const string ConfigDirectory = "Assets/Config";
    private const string ConfigPath = "Assets/Config/settings.json";

    public int ResolutionIndex { get; set; }
    public bool IsFullscreen { get; set; }
    public bool DebugModeEnabled { get; set; }
    public bool CombatDebugOverlayEnabled { get; set; }
    public bool DisableHealth { get; set; }
    public bool DisableCollision { get; set; }
    public bool DisableKeyRequirements { get; set; }

    public static GameOptions Defaults() => new()
    {
        ResolutionIndex = 0,
        IsFullscreen = false,
        DebugModeEnabled = false,
        CombatDebugOverlayEnabled = false,
        DisableHealth = false,
        DisableCollision = false,
        DisableKeyRequirements = false
    };

    public static GameOptions LoadOrCreateDefaults()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                var defaults = Defaults();
                Save(defaults);
                return defaults;
            }

            string json = File.ReadAllText(ConfigPath);
            GameOptions? loaded = JsonSerializer.Deserialize<GameOptions>(json);
            if (loaded is null)
            {
                var defaults = Defaults();
                Save(defaults);
                return defaults;
            }

            return loaded;
        }
        catch
        {
            var defaults = Defaults();
            Save(defaults);
            return defaults;
        }
    }

    public static void Save(GameOptions options)
    {
        Directory.CreateDirectory(ConfigDirectory);
        string json = JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
