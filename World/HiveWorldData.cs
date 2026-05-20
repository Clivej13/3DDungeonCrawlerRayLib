using System.Text.Json.Serialization;

namespace DungeonCrawler.World;

public sealed class LayerData
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public required int[][] Tiles { get; set; }
    public List<LayerNodeReference> Nodes { get; set; } = [];

    [JsonIgnore]
    public int Width => Tiles.Length == 0 ? 0 : Tiles[0].Length;

    [JsonIgnore]
    public int Height => Tiles.Length;
}

public sealed class LayerNodeReference
{
    public required string Id { get; set; }
    public required string File { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

public sealed class NodeData
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public required int[][] Tiles { get; set; }
    public List<NodeConnectionData> Connections { get; set; } = [];
    public List<NodeObjectData> Objects { get; set; } = [];
    public PlayerSpawnData? PlayerSpawn { get; set; }

    [JsonIgnore]
    public int Width => Tiles.Length == 0 ? 0 : Tiles[0].Length;

    [JsonIgnore]
    public int Height => Tiles.Length;
}

public sealed class NodeConnectionData
{
    public int TileX { get; set; }
    public int TileY { get; set; }
    public required string Target { get; set; }
}

public sealed class NodeObjectData
{
    public required string Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}
