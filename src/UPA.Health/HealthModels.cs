using UPA.ProjectModel;

namespace UPA.Health;

public enum FindingSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public sealed record HealthFinding(
    string Code,
    FindingSeverity Severity,
    string Title,
    string Evidence,
    string Recommendation);

public sealed record HealthScore(
    double Score,
    string Grade,
    IReadOnlyDictionary<string, double> CategoryScores);

public sealed record ArchitectureHealthReport(
    HealthScore Score,
    IReadOnlyList<HealthFinding> Findings,
    IReadOnlyList<string> ArchitectureSignals,
    DateTimeOffset GeneratedAtUtc,
    string AnalyzerVersion);
