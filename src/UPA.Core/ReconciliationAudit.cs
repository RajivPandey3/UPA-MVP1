using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UPA.Core;

public sealed record ReconciliationAudit(
    EntityId ProjectId,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    int BeforeCount,
    int AfterCount,
    IReadOnlyList<ReconciliationChange> Changes,
    string Fingerprint)
{
    public static ReconciliationAudit Create(EntityId projectId, DateTimeOffset startedAt,
        IReadOnlyCollection<ProjectKnowledgeNode> before,
        IReadOnlyCollection<ProjectKnowledgeNode> after)
    {
        var changes = ReconciliationEngine.Compare(before, after);
        var completedAt = DateTimeOffset.UtcNow;
        var canonical = JsonSerializer.Serialize(new { projectId, before, after, changes });
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return new ReconciliationAudit(projectId, startedAt, completedAt, before.Count, after.Count, changes, fingerprint);
    }
}
