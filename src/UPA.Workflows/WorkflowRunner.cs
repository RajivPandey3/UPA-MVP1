using UPA.Execution;

namespace UPA.Pipeline;

public interface IVerifiedTransaction
{
    string Preview { get; }
    IReadOnlyList<OutputExpectation> ExpectedOutputs { get; }
    void CheckPreconditions();
    void Execute(ApprovalToken approval);
    IReadOnlyList<string> VerifyOutput();
    void Rollback();
}

public sealed record ExecutionPreview(string PlanId, string Intent, string Changes, string ContentHash);

public sealed class WorkflowRunner
{
    private readonly List<PipelineEvent> _events = new();
    private readonly OutputVerifierRegistry _verifiers;
    public WorkflowRunner(OutputVerifierRegistry? verifiers = null)
        => _verifiers = verifiers ?? new OutputVerifierRegistry();

    public PipelineResult Execute(
        string runId,
        string projectRoot,
        string intent,
        IPlatformAdapter adapter,
        Func<ExecutionPreview, ApprovalToken?> requestApproval)
        => new WorkflowRunner(_verifiers).ExecuteRun(runId, projectRoot, intent, adapter, requestApproval);

    private PipelineResult ExecuteRun(
        string runId,
        string projectRoot,
        string intent,
        IPlatformAdapter adapter,
        Func<ExecutionPreview, ApprovalToken?> requestApproval)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(intent);
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(requestApproval);
        _events.Clear();
        IVerifiedTransaction? transaction = null;
        RunJournal? journal = null;
        var executionStarted = false;
        try
        {
            Emit(PipelineStage.Intake, "PIPE-001", "Intent accepted.");
            projectRoot = WorkspacePath.Root(projectRoot);
            var prepared = adapter.Prepare(projectRoot, intent);
            var plan = prepared.Plan;
            if (plan.PlatformId != adapter.Id || plan.Version != adapter.Version ||
                !adapter.Capabilities.Contains(plan.CapabilityId) || string.IsNullOrWhiteSpace(plan.PlanId))
                throw new InvalidOperationException("Unsupported or invalid platform capability.");
            transaction = prepared.Transaction;
            Emit(PipelineStage.Inspect, "PIPE-011", "Workspace inspected by " + adapter.Id);
            Emit(PipelineStage.Plan, "PIPE-030", "Platform plan prepared.");
            Emit(PipelineStage.Validate, "PIPE-041", "Platform plan validation passed.");
            transaction.CheckPreconditions();
            var preview = transaction.Preview;
            var outputs = transaction.ExpectedOutputs.ToArray();
            var contentHash = WorkflowFingerprint.Compute(projectRoot, plan, preview, outputs);
            var previewCreatedAt = DateTimeOffset.UtcNow;
            if (string.IsNullOrWhiteSpace(preview) || outputs.Length == 0)
                return Block(runId, "PIPE-050", "A concrete preview is required.");
            Emit(PipelineStage.Bind, "PIPE-071", "Plan bound to a transaction.");
            Emit(PipelineStage.Preview, "PIPE-051", preview);
            var approval = requestApproval(new ExecutionPreview(plan.PlanId, intent, $"Workspace: {projectRoot}\nPlatform: {plan.PlatformId}/{plan.CapabilityId}@{plan.Version}\n{preview}", contentHash));
            if (approval is null || !approval.ExplicitlyApproved || approval.PlanId != plan.PlanId ||
                approval.ContentHash != contentHash || approval.IssuedAtUtc < previewCreatedAt ||
                approval.IssuedAtUtc > DateTimeOffset.UtcNow.AddSeconds(30) ||
                DateTimeOffset.UtcNow - approval.IssuedAtUtc > TimeSpan.FromMinutes(5) ||
                string.IsNullOrWhiteSpace(approval.ApprovedBy))
            {
                Emit(PipelineStage.AwaitApproval, "PIPE-060", "Explicit approval for this preview is required.");
                return new PipelineResult(false, State(runId, PipelineStage.AwaitApproval,
                    false, true, false), new[] { "Transaction was not approved." });
            }
            transaction.CheckPreconditions();
            if (contentHash != WorkflowFingerprint.Compute(projectRoot, plan, transaction.Preview, transaction.ExpectedOutputs))
                throw new InvalidOperationException("Bound plan changed after preview/approval.");
            journal = new RunJournal(projectRoot, runId, contentHash, outputs);
            journal.Write("Executing", _events);
            executionStarted = true;
            transaction.Execute(approval);
            journal.Write("Verifying", _events);
            Emit(PipelineStage.Execute, "PIPE-081", "Executor returned; checking actual output.");
            transaction.VerifyOutput();
            var evidence = _verifiers.Verify(projectRoot, outputs);
            if (evidence.Count == 0 || evidence.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("Output verification did not provide evidence.");
            foreach (var observation in evidence)
                Emit(PipelineStage.Audit, "PIPE-090", observation);
            Emit(PipelineStage.Completed, "PIPE-100", "Execution and output verification completed.");
            journal.Write("Completed", _events);
            return new PipelineResult(true, State(runId, PipelineStage.Completed, false, true, false), evidence);
        }
        catch (Exception exception)
        {
            var message = exception.Message;
            var rolledBack = false;
            if (executionStarted && transaction != null)
            {
                try
                {
                    transaction.Rollback();
                    rolledBack = true;
                    Emit(PipelineStage.Audit, "PIPE-083", "Transaction rollback completed.");
                }
                catch (Exception rollbackException)
                {
                    message += " Rollback failed: " + rollbackException.Message;
                }
            }
            var result = Block(runId, "PIPE-080", message);
            if (journal != null)
            {
                try { journal.Write(rolledBack ? "RolledBack" : "Incomplete", _events, message); }
                catch (Exception journalException)
                {
                    return Block(runId, "PIPE-084", message + " Journal update failed: " + journalException.Message);
                }
            }
            return result;
        }
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


