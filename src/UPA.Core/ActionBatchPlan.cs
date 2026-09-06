namespace UPA.Core;

public sealed record ActionBatchItem(string FindingId, ActionMode Mode, string Owner);

public sealed record ActionBatchPlan(string PlanId, IReadOnlyList<ActionBatchItem> Items, bool ExplicitlyApproved)
{
    public static ActionBatchPlan Create(string planId, IEnumerable<ActionFinding> findings, string owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planId);
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentNullException.ThrowIfNull(findings);
        return new ActionBatchPlan(planId, findings.Select(finding => new ActionBatchItem(finding.FindingId, finding.Mode, owner)).ToArray(), false);
    }

    public ActionBatchPlan Approve() => this with { ExplicitlyApproved = true };

    public void EnsureExecutable()
    {
        if (!ExplicitlyApproved) throw new InvalidOperationException("Action batch requires explicit approval.");
        if (Items.Any(item => item.Mode == ActionMode.Unknown)) throw new InvalidOperationException("Unknown findings cannot be executed.");
    }
}
