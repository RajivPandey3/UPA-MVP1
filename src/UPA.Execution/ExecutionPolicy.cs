namespace UPA.Execution;

public static class ExecutionPolicy
{
    public const string Version = "1.0";

    public static bool CanExecute(
        ApprovalToken? approval,
        string planId,
        bool dryRun)
    {
        if (dryRun)
            return true;

        return approval is not null &&
               approval.ExplicitlyApproved &&
               string.Equals(approval.PlanId, planId, StringComparison.Ordinal);
    }

    public static string MutationBoundary =>
        "MVP-1: only allowlisted text-file mutations inside the sandbox are executable.";
}
