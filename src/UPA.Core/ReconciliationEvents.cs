namespace UPA.Core;

public sealed record ReconciliationEvent(
    EntityId ProjectId,
    ReconciliationChange Change,
    DateTimeOffset EmittedAt,
    string EventId);

public static class ReconciliationEventFactory
{
    public static IReadOnlyList<ReconciliationEvent> Create(EntityId projectId,
        IReadOnlyCollection<ProjectKnowledgeNode> before,
        IReadOnlyCollection<ProjectKnowledgeNode> after,
        DateTimeOffset emittedAt)
    {
        return ReconciliationEngine.Compare(before, after)
            .Where(change => change.Kind != ReconciliationChangeKind.Unchanged)
            .Select(change => new ReconciliationEvent(
                projectId, change, emittedAt,
                EntityId.FromStableKey($"{projectId.Value}:{change.NativeIdentity}:{change.Kind}:{emittedAt:O}").Value))
            .ToArray();
    }
}
