using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;
using Microsoft.Xna.Framework;

namespace Fida.UI.Campaign;

/// <summary>Presentation-only composition; it never changes campaign prerequisites or saves.</summary>
public static class CampaignRouteDiagram
{
    private static readonly Dictionary<string, Vector2> ReferencePositions = new()
    {
        ["A1"] = new(0,-265), ["A2"] = new(-210,0), ["A3"] = new(210,0), ["AG"] = new(0,265)
    };
    public static readonly (string From, string To)[] ReferenceEdges =
        { ("A1","A2"), ("A1","A3"), ("A2","AG"), ("A3","AG") };

    public static bool IsReference(IReadOnlyList<CampaignNode> nodes) => nodes.Count == 4 &&
        nodes.All(node => ReferencePositions.ContainsKey(node.Id));

    public static float WidthScale(string id) => id is "A2" or "A3" ? .89f : 1;

    public static bool TryArrange(IReadOnlyList<CampaignNode> nodes, IDictionary<string, Vector2> positions)
    {
        if (!IsReference(nodes)) return false;
        foreach (var node in nodes) positions[node.Id] = ReferencePositions[node.Id];
        return true;
    }
}
