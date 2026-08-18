namespace UPA.Planning;

public enum PlanActionKind
{
    Inspect,
    Create,
    Configure,
    Link,
    GeneratePlaceholder,
    Validate,
    Report,
    AwaitUserInput
}

public enum PlanRisk
{
    Low,
    Medium,
    High,
    Critical
}

public sealed record PlanInput(
    string Key,
    string Description,
    bool Required,
    string? DefaultValue = null);

public sealed record PlanPrecondition(
    string Code,
    string Description,
    bool Blocking);

public sealed record PlanAction(
    string Id,
    PlanActionKind Kind,
    string Target,
    string Description,
    IReadOnlyList<string> DependsOn,
    IReadOnlyList<PlanPrecondition> Preconditions,
    PlanRisk Risk,
    double Confidence,
    bool RequiresApproval,
    bool CanUsePlaceholder);

public sealed record PlanUnknown(
    string Key,
    string Description,
    bool Blocking,
    string Resolution);

public sealed record UpaPlan(
    string PlanId,
    string Intent,
    IReadOnlyList<PlanInput> Inputs,
    IReadOnlyList<PlanAction> Actions,
    IReadOnlyList<PlanUnknown> Unknowns,
    bool RequiresApproval,
    bool Executable,
    string GrammarVersion);
