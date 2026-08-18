namespace UPA.Adapter;

public enum AdapterExecutor
{
    Scene,
    Component,
    Asset,
    Prefab,
    Settings,
    Importer,
    Validation
}

public sealed record OperationArguments(
    IReadOnlyDictionary<string, object?> Values);

public sealed record AdapterPrecondition(
    string Code,
    string Description,
    bool Blocking);

public sealed record BoundOperation(
    string OperationId,
    AdapterExecutor Executor,
    OperationArguments Arguments,
    IReadOnlyList<AdapterPrecondition> Preconditions,
    IReadOnlyList<string> DependsOn,
    double Confidence,
    bool RequiresApproval);

public sealed record AdapterIssue(
    string Code,
    string Severity,
    string Message,
    string? OperationId = null);

public sealed record ExecutionBatch(
    string BatchId,
    IReadOnlyList<BoundOperation> Operations,
    bool RequiresApproval);

public sealed record BoundExecutionPlan(
    string PlanId,
    IReadOnlyList<ExecutionBatch> Batches,
    IReadOnlyList<AdapterIssue> Issues,
    bool ReadyForPreview,
    bool ReadyForExecution);
