using Raylib_cs;
using System.Text.Json;

namespace DungeonCrawler.World;

public sealed class NodeManager
{
    private readonly LayerManager _layerManager;
    private readonly Texture2D _goblinTexture;
    private readonly Texture2D _silverKeyTexture;
    private readonly Texture2D _goldKeyTexture;

    public string CurrentNodeId { get; private set; }
    public DungeonMap CurrentMap { get; private set; }

    public NodeManager(LayerManager layerManager, string startNodeId, Texture2D goblinTexture, Texture2D silverKeyTexture, Texture2D goldKeyTexture)
    {
        _layerManager = layerManager;
        _goblinTexture = goblinTexture;
        _silverKeyTexture = silverKeyTexture;
        _goldKeyTexture = goldKeyTexture;

        CurrentNodeId = startNodeId;
        CurrentMap = LoadNodeAsDungeonMap(startNodeId);
    }

    public bool TryTransition(string targetNodeId, out DungeonMap map)
    {
        map = CurrentMap;
        if (!_layerManager.CurrentLayer.Nodes.Any(n => n.Id.Equals(targetNodeId, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        CurrentNodeId = targetNodeId;
        CurrentMap = LoadNodeAsDungeonMap(targetNodeId);
        map = CurrentMap;
        return true;
    }

    private DungeonMap LoadNodeAsDungeonMap(string nodeId)
    {
        LayerNodeReference nodeRef = _layerManager.FindNodeReference(nodeId)
            ?? throw new InvalidOperationException($"Node '{nodeId}' is not placed on layer '{_layerManager.CurrentLayer.Id}'.");

        NodeData node = LoadNodeData(nodeRef.File);
        return DungeonMap.FromNode(node, _goblinTexture, _silverKeyTexture, _goldKeyTexture);
    }

    private static NodeData LoadNodeData(string nodePath)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        string json = File.ReadAllText(nodePath);
        NodeData? node = JsonSerializer.Deserialize<NodeData>(json, options);

        if (node is null || node.Tiles.Length == 0)
        {
            throw new InvalidOperationException($"Failed to load node data from '{nodePath}'.");
        }

        int width = node.Tiles[0].Length;
        if (width == 0 || node.Tiles.Any(row => row.Length != width))
        {
            throw new InvalidOperationException($"Node '{node.Id}' has inconsistent tile row widths.");
        }

        return node;
    }
}
