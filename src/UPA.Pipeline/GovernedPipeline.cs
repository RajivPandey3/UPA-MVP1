namespace UPA.Pipeline;

public sealed partial class GovernedPipeline
{
    private readonly List<PipelineEvent> _events = new();

    public PipelineResult Start(
        string runId,
        string intent,
        bool projectModelAvailable,
        bool healthPassed,
        bool planValid,
        bool previewAccepted,
        bool explicitlyApproved,
        bool adapterReady,
        bool executorSucceeded)
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("RunId is required.", nameof(runId));

        if (string.IsNullOrWhiteSpace(intent))
            throw new ArgumentException("Intent is required.", nameof(intent));

        _events.Clear();

        Emit(PipelineStage.Intake, "PIPE-001", "Intent accepted.");

        if (!projectModelAvailable)
            return Block(runId, "PIPE-010", "ProjectModel is unavailable.");

        Emit(PipelineStage.Inspect, "PIPE-011", "ProjectModel available.");

        if (!healthPassed)
            return Block(runId, "PIPE-020", "Health gate failed.");

        Emit(PipelineStage.Analyze, "PIPE-021", "Health gate passed.");

        Emit(PipelineStage.Plan, "PIPE-030", "Plan generated.");

        if (!planValid)
            return Block(runId, "PIPE-040", "Plan validation failed.");

        Emit(PipelineStage.Validate, "PIPE-041", "Plan validation passed.");

        if (!previewAccepted)
            return Block(runId, "PIPE-050", "Preview was not accepted.");

        Emit(PipelineStage.Preview, "PIPE-051", "Preview accepted.");

        if (!explicitlyApproved)
        {
            Emit(PipelineStage.AwaitApproval, "PIPE-060",
                "Explicit human approval is required.");

            return new PipelineResult(
                Success: false,
                State: State(
                    runId,
                    PipelineStage.AwaitApproval,
                    blocking: false,
                    approvalRequired: true,
                    executionAuthorized: false),
                Findings: new[]
                {
                    "Pipeline is waiting for explicit approval."
                });
        }

        Emit(PipelineStage.AwaitApproval, "PIPE-061",
            "Explicit approval recorded.");

        if (!adapterReady)
            return Block(runId, "PIPE-070",
                "Plan-to-executor adapter rejected the plan.");

        Emit(PipelineStage.Bind, "PIPE-071",
            "Plan bound to allowlisted executors.");

        if (!executorSucceeded)
            return Block(runId, "PIPE-080",
                "Executor reported failure; transaction policy should roll back.");

        return Block(runId, "PIPE-082",
            "Caller status flags cannot prove execution. Use the executing pipeline with output verification.");
    }

    private PipelineResult Block(
        string runId,
        string code,
        string message)
    {
        Emit(PipelineStage.Blocked, code, message);

        return new PipelineResult(
            false,
            State(
                runId,
                PipelineStage.Blocked,
                blocking: true,
                approvalRequired: false,
                executionAuthorized: false),
            new[] { message });
    }

    private PipelineState State(
        string runId,
        PipelineStage stage,
        bool blocking,
        bool approvalRequired,
        bool executionAuthorized)
        => new(
            runId,
            stage,
            blocking,
            approvalRequired,
            executionAuthorized,
            _events.ToArray());

    private void Emit(
        PipelineStage stage,
        string code,
        string message)
    {
        _events.Add(new PipelineEvent(
            DateTimeOffset.UtcNow,
            stage,
            code,
            message));
    }
}
