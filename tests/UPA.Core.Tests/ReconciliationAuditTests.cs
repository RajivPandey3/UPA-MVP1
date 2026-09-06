using System;
using System.Linq;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ReconciliationAuditTests
{
    [Fact]
    public void CreatesDeterministicFingerprintForSameSnapshots()
    {
        var before = new[] { Node("one", "one.txt") };
        var after = new[] { Node("one", "one.txt"), Node("two", "two.txt") };
        var start = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var first = ReconciliationAudit.Create(EntityId.FromStableKey("project"), start, before, after);
        var second = ReconciliationAudit.Create(EntityId.FromStableKey("project"), start, before, after);
        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.Equal(ReconciliationChangeKind.Added, first.Changes.Single(change => change.NativeIdentity == "two").Kind);
    }

    private static ProjectKnowledgeNode Node(string identity, string location) =>
        new(EntityId.FromStableKey(identity), KnowledgeDimension.Project, identity, "Asset", location, EvidenceStatus.Confirmed, DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
}
