namespace UPA.Core;

public enum ActionResultStatus
{
    Applied,
    Skipped,
    Failed
}

public sealed record ActionExecutionResult(string FindingId, ActionResultStatus Status, string Evidence);

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
