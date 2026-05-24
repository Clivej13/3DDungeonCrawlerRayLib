namespace DungeonCrawler.World;

public sealed class EnemySpawnData
{
    public required string Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class KeySpawnData
{
    public required string Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class DoorSpawnData
{
    public required string Id { get; set; }
    public string? RequiredKeyId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public bool Locked { get; set; }
}

public sealed class ExitData
{
    public float X { get; set; }
    public float Y { get; set; }
    public bool RequiresAllKeys { get; set; } = true;
}
