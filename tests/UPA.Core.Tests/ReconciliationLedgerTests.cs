using System;
using System.Linq;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ReconciliationLedgerTests
{
    [Fact]
    public void DeduplicatesReplayAndPreservesOrder()
    {
        var project = EntityId.FromStableKey("project");
        var change = new ReconciliationChange(ReconciliationChangeKind.Added, "asset", null, null, "new");
        var first = new ReconciliationEvent(project, change, DateTimeOffset.UtcNow, "event-1");
        var second = new ReconciliationEvent(project, change, DateTimeOffset.UtcNow, "event-2");
        var ledger = new ReconciliationEventLedger();
        Assert.Equal(2, ledger.Append(new[] { first, second, first }));
        Assert.Equal(new[] { "event-1", "event-2" }, ledger.Events.Select(item => item.EventId));
    }
}
