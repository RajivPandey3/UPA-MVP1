using System;
using System.IO;
using System.Linq;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ReconciliationLedgerStoreTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("upa-ledger-").FullName;

    [Fact]
    public void ReloadPreservesEventsAndDeduplication()
    {
        var eventItem = new ReconciliationEvent(EntityId.New(), new ReconciliationChange(ReconciliationChangeKind.Added, "a", null, null, "new"), DateTimeOffset.UtcNow, "e1");
        var ledger = new ReconciliationEventLedger();
        ledger.Append(new[] { eventItem, eventItem });
        var path = Path.Combine(root, "ledger.json");
        var store = new ReconciliationLedgerStore();
        store.Save(path, ledger);
        var loaded = store.Load(path);
        Assert.Equal(new[] { "e1" }, loaded.Events.Select(item => item.EventId));
    }

    public void Dispose() => Directory.Delete(root, true);
}
