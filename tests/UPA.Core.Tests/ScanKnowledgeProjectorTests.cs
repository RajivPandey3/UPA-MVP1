using System;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ScanKnowledgeProjectorTests
{
    [Fact]
    public void ProjectsScanInventoryIntoStableKnowledgeNodes()
    {
        var projectId = EntityId.FromStableKey("project");
        var scan = new ScanResult(projectId, DateTimeOffset.Parse("2026-01-01T00:00:00Z"), Array.Empty<Diagnostic>())
        {
            ProjectRoot = "C:/project",
            Packages = new[] { new PackageInfo("com.example", "1.0") },
            AssetPaths = new[] { "Assets/player.prefab" }
        };
        var nodes = ScanKnowledgeProjector.Project(scan);
        Assert.Contains(nodes, node => node.NativeIdentity == projectId.Value);
        Assert.Contains(nodes, node => node.NativeIdentity == "package:com.example");
        Assert.Contains(nodes, node => node.NativeIdentity == "asset:Assets/player.prefab");
    }
}
