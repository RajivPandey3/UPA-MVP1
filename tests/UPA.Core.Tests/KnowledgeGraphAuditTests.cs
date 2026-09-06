using System;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class KnowledgeGraphAuditTests
{
    [Fact]
    public void AuditsGraphNodeChangesAndCounts()
    {
        var before = Graph(new[] { "one" });
        var after = Graph(new[] { "one", "two" });
        var audit = KnowledgeGraphAudit.Create(before, after);
        Assert.Equal(2, audit.BeforeNodes);
        Assert.Equal(3, audit.AfterNodes);
        Assert.Contains(audit.NodeChanges, change => change.Kind == ReconciliationChangeKind.Added);
        Assert.False(string.IsNullOrWhiteSpace(audit.Fingerprint));
    }

    private static ScanKnowledgeProjector.KnowledgeGraph Graph(string[] identities)
    {
        var scan = new ScanResult(EntityId.FromStableKey("p"), DateTimeOffset.UtcNow, Array.Empty<Diagnostic>())
        { ProjectRoot = "root", AssetPaths = identities };
        return ScanKnowledgeProjector.ProjectGraph(scan);
    }
}
