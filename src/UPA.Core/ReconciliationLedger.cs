namespace UPA.Core;

public sealed class ReconciliationEventLedger
{
    private readonly HashSet<string> eventIds = new(StringComparer.Ordinal);
    private readonly List<ReconciliationEvent> events = new();

    public IReadOnlyList<ReconciliationEvent> Events => events.ToArray();

    public int Append(IEnumerable<ReconciliationEvent> incoming)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        var added = 0;
        foreach (var item in incoming)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.EventId)) throw new InvalidDataException("Reconciliation event identity is required.");
            if (eventIds.Add(item.EventId)) { events.Add(item); added++; }
        }
        return added;
    }
}
