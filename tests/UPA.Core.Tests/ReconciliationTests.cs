using System;
using System.Linq;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ReconciliationTests
{
    [Fact]
    public void DetectsAddedRemovedAndChangedByNativeIdentity()
    {
        var before = new[] { Node("a", "old.txt"), Node("removed", "gone.txt") };
        var after = new[] { Node("a", "new.txt"), Node("added", "new.txt") };
        var changes = ReconciliationEngine.Compare(before, after);
        Assert.Equal(ReconciliationChangeKind.Changed, changes.Single(x => x.NativeIdentity == "a").Kind);
        Assert.Equal(ReconciliationChangeKind.Removed, changes.Single(x => x.NativeIdentity == "removed").Kind);
        Assert.Equal(ReconciliationChangeKind.Added, changes.Single(x => x.NativeIdentity == "added").Kind);
    }

    [Fact]
    public void FlagsDuplicateNativeIdentityAsConflict()
    {
        var changes = ReconciliationEngine.Compare(new[] { Node("dup", "a"), Node("dup", "b") }, new[] { Node("dup", "a") });
        Assert.Equal(ReconciliationChangeKind.Conflict, Assert.Single(changes).Kind);
    }

    private static ProjectKnowledgeNode Node(string identity, string location) =>
        new(EntityId.FromStableKey(identity), KnowledgeDimension.Project, identity, "Asset", location, EvidenceStatus.Confirmed, DateTimeOffset.UtcNow);
}
