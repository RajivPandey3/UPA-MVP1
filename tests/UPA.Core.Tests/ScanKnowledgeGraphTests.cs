using System;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ScanKnowledgeGraphTests
{
    [Fact]
    public void LinksEveryProjectedItemToProjectOwner()
    {
        var scan = new ScanResult(EntityId.FromStableKey("p"), DateTimeOffset.UtcNow, Array.Empty<Diagnostic>())
        {
            ProjectRoot = "root",
            Packages = new[] { new PackageInfo("pkg", "1") },
            AssetPaths = new[] { "Assets/a" }
        };
        var graph = ScanKnowledgeProjector.ProjectGraph(scan);
        Assert.Equal(graph.Nodes.Count - 1, graph.Edges.Count);
        Assert.All(graph.Edges, edge => Assert.Equal(RelationshipKind.Contains, edge.Kind));
    }
}
