using System;
using System.Linq;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ReconciliationEventsTests
{
    [Fact]
    public void EmitsOnlyActionableChangesWithStableEventIds()
    {
        var project = EntityId.FromStableKey("project");
        var before = new[] { Node("same", "same"), Node("gone", "gone") };
        var after = new[] { Node("same", "same"), Node("new", "new") };
        var time = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var first = ReconciliationEventFactory.Create(project, before, after, time);
        var second = ReconciliationEventFactory.Create(project, before, after, time);
        Assert.Equal(2, first.Count);
        Assert.Equal(first.Select(item => item.EventId), second.Select(item => item.EventId));
    }

    private static ProjectKnowledgeNode Node(string identity, string location) =>
        new(EntityId.FromStableKey(identity), KnowledgeDimension.Project, identity, "Asset", location, EvidenceStatus.Confirmed, DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
}
