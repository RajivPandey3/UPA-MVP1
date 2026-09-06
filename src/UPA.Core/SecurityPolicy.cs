namespace UPA.Core;

public enum VulnerabilitySeverity
{
    Low,
    Moderate,
    High,
    Critical
}

public sealed record VulnerabilityFinding(string Id, VulnerabilitySeverity Severity, string Package, string Evidence);

public static class SecurityPolicy
{
    public static bool BlocksRelease(IEnumerable<VulnerabilityFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);
        return findings.Any(finding => finding.Severity is VulnerabilitySeverity.High or VulnerabilitySeverity.Critical);
    }
}
