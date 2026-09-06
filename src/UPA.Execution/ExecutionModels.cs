namespace UPA.Execution;

public enum MutationKind
{
    CreateTextFile,
    ReplaceTextFile
}

public sealed record MutationRequest(
    string OperationId,
    MutationKind Kind,
    string RelativePath,
    string Content);

public sealed record FileSnapshot(
    string RelativePath,
    bool Exists,
    string? Content);

public sealed record ApprovalToken(
    string PlanId,
    string ApprovedBy,
    DateTimeOffset IssuedAtUtc,
    bool ExplicitlyApproved)
{
    public string? ContentHash { get; init; }
}

public sealed record ExecutionPrecondition(
    string Code,
    string Description,
    Func<bool> Check);

public sealed record AuditEntry(
    DateTimeOffset TimestampUtc,
    string OperationId,
    string Event,
    string Detail);

public sealed record TransactionResult(
    bool Success,
    bool DryRun,
    bool RolledBack,
    IReadOnlyList<AuditEntry> Audit,
    IReadOnlyList<string> Errors);
