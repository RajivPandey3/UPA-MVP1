namespace UPA.Health;

public static class HealthGovernance
{
    public static bool IsSafeForPlanning(ArchitectureHealthReport report)
        => !report.Findings.Any(x => x.Severity == FindingSeverity.Critical);

    public static bool IsSafeForAutofix(ArchitectureHealthReport report)
        => false; // MVP-1 deliberately has no autonomous mutation permission.

    public static string ExplainMutationBoundary()
        => "Health analysis may recommend changes, but MVP-1 cannot apply them automatically.";
}
