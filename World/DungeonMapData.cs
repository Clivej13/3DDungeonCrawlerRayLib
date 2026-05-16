namespace DungeonCrawler.World;

public sealed class DungeonMapData
{
    public int TileSize { get; set; } = 64;
    public required string[] Rows { get; set; }
    public required PlayerSpawnData PlayerSpawn { get; set; }
    public List<EnemySpawnData> Enemies { get; set; } = [];
    public List<KeySpawnData> Keys { get; set; } = [];
    public List<DoorSpawnData> Doors { get; set; } = [];
    public ExitData? Exit { get; set; }
}

public sealed class PlayerSpawnData
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Angle { get; set; }
}

public sealed class EnemySpawnData
{
    public required string Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class KeySpawnData
{
    public required string Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class DoorSpawnData
{
    public required string Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public bool Locked { get; set; } = true;
}

public sealed class ExitData
{
    public float X { get; set; }
    public float Y { get; set; }
    public bool RequiresAllKeys { get; set; } = true;
}
