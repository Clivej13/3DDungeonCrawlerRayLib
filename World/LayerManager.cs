using System.Text.Json;

namespace DungeonCrawler.World;

public sealed class LayerManager
{
    public LayerData CurrentLayer { get; }

    public LayerManager(string layerPath)
    {
        CurrentLayer = LoadLayer(layerPath);
    }

    public LayerNodeReference? FindNodeReference(string nodeId)
        => CurrentLayer.Nodes.FirstOrDefault(n => n.Id.Equals(nodeId, StringComparison.OrdinalIgnoreCase));

    private static LayerData LoadLayer(string layerPath)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        string json = File.ReadAllText(layerPath);
        LayerData? layer = JsonSerializer.Deserialize<LayerData>(json, options);

        if (layer is null || layer.Tiles.Length == 0)
        {
            throw new InvalidOperationException($"Failed to load layer data from '{layerPath}'.");
        }

        int width = layer.Tiles[0].Length;
        if (width == 0 || layer.Tiles.Any(row => row.Length != width))
        {
            throw new InvalidOperationException($"Layer '{layer.Id}' has inconsistent tile row widths.");
        }

        return layer;
    }
}
