using UPA.Planning;

namespace UPA.Governance;

public enum ValidationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public sealed record PlanValidationIssue(
    string Code,
    ValidationSeverity Severity,
    string Message,
    string? ActionId = null);

public sealed record PlanValidationResult(
    bool IsValid,
    bool CanEnterApproval,
    IReadOnlyList<PlanValidationIssue> Issues);

public sealed record PreviewChange(
    string ActionId,
    string Target,
    string Operation,
    string Risk,
    double Confidence,
    bool RequiresApproval);

public sealed record RiskSummary(
    string OverallRisk,
    int Low,
    int Medium,
    int High,
    int Critical,
    double AverageConfidence);

public sealed record ApprovalPacket(
    string PlanId,
    string Intent,
    PlanValidationResult Validation,
    IReadOnlyList<PreviewChange> Preview,
    RiskSummary Risk,
    string MutationPolicy,
    bool ExecutionAuthorized);
