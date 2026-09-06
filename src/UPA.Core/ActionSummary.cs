namespace UPA.Core;

public sealed record ActionSummary(
    int Applied,
    int Skipped,
    int Failed,
    bool RollbackRequired,
    string NextAction)
{
    public static ActionSummary Create(IReadOnlyList<ActionExecutionResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        var applied = results.Count(result => result.Status == ActionResultStatus.Applied);
        var skipped = results.Count(result => result.Status == ActionResultStatus.Skipped);
        var failed = results.Count(result => result.Status == ActionResultStatus.Failed);
        return new ActionSummary(applied, skipped, failed, failed > 0 && applied > 0,
            failed > 0 ? "Review failed items and rollback evidence." : skipped > 0 ? "Review skipped items." : "No further action required.");
    }
}
