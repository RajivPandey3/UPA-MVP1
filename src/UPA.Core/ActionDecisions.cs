namespace UPA.Core;

public enum ActionMode
{
    Auto,
    Assist,
    Human,
    Unknown
}

public sealed record ActionFinding(
    string FindingId,
    string Description,
    EvidenceStatus Evidence,
    bool Deterministic,
    bool LowRisk,
    ActionMode Mode);

public static class ActionDecisionPolicy
{
    public static ActionFinding Classify(string findingId, string description, EvidenceStatus evidence,
        bool deterministic, bool lowRisk)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(findingId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        var mode = evidence is EvidenceStatus.Unknown or EvidenceStatus.Conflicted or EvidenceStatus.Stale
            ? ActionMode.Unknown
            : deterministic && lowRisk ? ActionMode.Auto
            : deterministic ? ActionMode.Assist
            : ActionMode.Human;
        return new ActionFinding(findingId, description, evidence, deterministic, lowRisk, mode);
    }
}
