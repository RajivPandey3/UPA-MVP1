namespace UPA.Governance;

public static class GovernancePolicy
{
    public const string Version = "1.0";

    public static bool IsExecutionAuthorized(ApprovalPacket packet)
        => false;

    public static string Explain()
        => "MVP-1 approval is descriptive only. Execution authorization is deliberately unavailable.";
}
