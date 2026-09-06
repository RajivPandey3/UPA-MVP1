namespace UPA.Core;

public enum ActionResultStatus
{
    Applied,
    Skipped,
    Failed
}

public sealed record ActionExecutionResult(string FindingId, ActionResultStatus Status, string Evidence);

public sealed record ActionBatchOutcome(IReadOnlyList<ActionExecutionResult> Results, bool Succeeded, bool PartialFailure)
{
    public static ActionBatchOutcome From(IReadOnlyList<ActionExecutionResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        var failures = results.Count(result => result.Status == ActionResultStatus.Failed);
        var applied = results.Count(result => result.Status == ActionResultStatus.Applied);
        return new ActionBatchOutcome(results, failures == 0, failures > 0 && applied > 0);
    }
}

public static class ActionBatchExecutor
{
    public static IReadOnlyList<ActionExecutionResult> Execute(
        ActionBatchPlan plan,
        Func<ActionBatchItem, ActionExecutionResult> apply)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(apply);
        plan.EnsureExecutable();
        return plan.Items.Select(item => apply(item)).ToArray();
    }
}
