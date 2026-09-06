using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UPA.Core;

public sealed record KnowledgeGraphAudit(
    int BeforeNodes,
    int AfterNodes,
    int BeforeEdges,
    int AfterEdges,
    IReadOnlyList<ReconciliationChange> NodeChanges,
    string Fingerprint)
{
    public static KnowledgeGraphAudit Create(
        ScanKnowledgeProjector.KnowledgeGraph before,
        ScanKnowledgeProjector.KnowledgeGraph after)
    {
        var nodeChanges = ReconciliationEngine.Compare(before.Nodes, after.Nodes);
        var canonical = JsonSerializer.Serialize(new { before, after, nodeChanges });
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return new KnowledgeGraphAudit(before.Nodes.Count, after.Nodes.Count, before.Edges.Count,
            after.Edges.Count, nodeChanges, fingerprint);
    }
}
