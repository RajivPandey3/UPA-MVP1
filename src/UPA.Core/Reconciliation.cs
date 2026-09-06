namespace UPA.Core;

public enum ReconciliationChangeKind
{
    Added,
    Removed,
    Changed,
    Unchanged,
    Conflict
}

public sealed record ReconciliationChange(
    ReconciliationChangeKind Kind,
    string NativeIdentity,
    ProjectKnowledgeNode? Before,
    ProjectKnowledgeNode? After,
    string Reason);

public static class ReconciliationEngine
{
    public static IReadOnlyList<ReconciliationChange> Compare(
        IReadOnlyCollection<ProjectKnowledgeNode> before,
        IReadOnlyCollection<ProjectKnowledgeNode> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        var oldByIdentity = before.GroupBy(node => node.NativeIdentity, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var newByIdentity = after.GroupBy(node => node.NativeIdentity, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var identities = oldByIdentity.Keys.Union(newByIdentity.Keys, StringComparer.Ordinal).OrderBy(identity => identity, StringComparer.Ordinal);
        var changes = new List<ReconciliationChange>();
        foreach (var identity in identities)
        {
            oldByIdentity.TryGetValue(identity, out var oldNodes);
            newByIdentity.TryGetValue(identity, out var newNodes);
            if (oldNodes is null) { changes.Add(new(ReconciliationChangeKind.Added, identity, null, newNodes![0], "Identity is new.")); continue; }
            if (newNodes is null) { changes.Add(new(ReconciliationChangeKind.Removed, identity, oldNodes[0], null, "Identity is missing.")); continue; }
            if (oldNodes.Length != 1 || newNodes.Length != 1)
            {
                changes.Add(new(ReconciliationChangeKind.Conflict, identity, oldNodes[0], newNodes[0], "Native identity is not unique."));
                continue;
            }
            var oldNode = oldNodes[0];
            var newNode = newNodes[0];
            var equivalent = oldNode.Id == newNode.Id && oldNode.Dimension == newNode.Dimension &&
                oldNode.NativeIdentity == newNode.NativeIdentity && oldNode.Type == newNode.Type &&
                oldNode.Location == newNode.Location && oldNode.Status == newNode.Status;
            changes.Add(equivalent
                ? new(ReconciliationChangeKind.Unchanged, identity, oldNode, newNode, "No observable change.")
                : new(ReconciliationChangeKind.Changed, identity, oldNode, newNode, "Observed state changed."));
        }
        return changes;
    }
}
