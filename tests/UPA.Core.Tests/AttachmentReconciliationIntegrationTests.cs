using System;
using System.IO;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class AttachmentReconciliationIntegrationTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("upa-e2e-").FullName;

    [Fact]
    public void AttachmentChangeProducesPersistentReplaySafeEvidence()
    {
        var project = EntityId.FromStableKey("consumer");
        var before = Graph(project, new[] { "Assets/player.prefab" });
        var after = Graph(project, new[] { "Assets/player.prefab", "Assets/enemy.prefab" });
        var events = ReconciliationEventFactory.Create(project, before.Nodes, after.Nodes, DateTimeOffset.UtcNow);
        var ledger = new ReconciliationEventLedger();
        Assert.Equal(1, ledger.Append(events));
        var path = Path.Combine(root, "events.json");
        new ReconciliationLedgerStore().Save(path, ledger);
        var restored = new ReconciliationLedgerStore().Load(path);
        Assert.Single(restored.Events);
        Assert.Equal(ReconciliationChangeKind.Added, restored.Events[0].Change.Kind);
    }

    private static ScanKnowledgeProjector.KnowledgeGraph Graph(EntityId project, string[] assets)
    {
        var scan = new ScanResult(project, DateTimeOffset.Parse("2026-01-01T00:00:00Z"), Array.Empty<Diagnostic>())
        { ProjectRoot = "root", AssetPaths = assets };
        return ScanKnowledgeProjector.ProjectGraph(scan);
    }

    public void Dispose() => Directory.Delete(root, true);
}
