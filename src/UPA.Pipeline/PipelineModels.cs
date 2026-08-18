namespace UPA.Pipeline;

public enum PipelineStage
{
    Intake,
    Inspect,
    Analyze,
    Plan,
    Validate,
    Preview,
    AwaitApproval,
    Bind,
    Execute,
    Audit,
    Completed,
    Blocked
}

public sealed record PipelineEvent(
    DateTimeOffset TimestampUtc,
    PipelineStage Stage,
    string Code,
    string Message);

public sealed record PipelineState(
    string RunId,
    PipelineStage Stage,
    bool HasBlockingIssue,
    bool ApprovalRequired,
    bool ExecutionAuthorized,
    IReadOnlyList<PipelineEvent> Events);

public sealed record PipelineResult(
    bool Success,
    PipelineState State,
    IReadOnlyList<string> Findings);
