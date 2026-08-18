namespace UPA.Operations;

public enum OperationRisk
{
    Low,
    Medium,
    High,
    Critical
}

public enum ExecutorFamily
{
    Scene,
    Component,
    Asset,
    Prefab,
    ProjectSettings,
    Importer,
    Validation
}

public sealed record OperationParameter(
    string Name,
    string Type,
    bool Required,
    string Description);

public sealed record OperationPrecondition(
    string Code,
    string Description,
    bool Blocking);

public sealed record OperationDefinition(
    string Id,
    string DisplayName,
    ExecutorFamily Executor,
    OperationRisk Risk,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<OperationParameter> Parameters,
    IReadOnlyList<OperationPrecondition> Preconditions,
    IReadOnlyList<string> DependsOn,
    string PreviewTemplate,
    bool SupportsDryRun,
    bool RequiresApproval);
