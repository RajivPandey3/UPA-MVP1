namespace UPA.Core;

public sealed record ActionRollbackResult(string FindingId, bool RolledBack, string Evidence);

public static class ActionRollback
{
    public static IReadOnlyList<ActionRollbackResult> Rollback(
        IReadOnlyList<ActionExecutionResult> results,
        Func<string, ActionRollbackResult> rollback)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(rollback);
        return results.Where(result => result.Status == ActionResultStatus.Applied)
            .Reverse().Select(result => rollback(result.FindingId)).ToArray();
    }
}
