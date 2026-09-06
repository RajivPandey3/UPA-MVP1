using UPA.Core;
using UPA.Execution;
using UPA.Planning;

namespace UPA.Pipeline.Tests;

public sealed class ExecutingPipelineTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("upa-pipeline-").FullName;

    [Fact]
    public void ActualWriteIsReadBackBeforeCompletion()
    {
        var binder = new FileBinder(_root);
        var result = Run(binder, Approve);
        Assert.True(result.Success, string.Join("; ", result.Findings));
        Assert.Equal("proof", File.ReadAllText(Path.Combine(_root, "proof.txt")));
        Assert.Contains(result.Findings, finding => finding.Contains("Verified proof.txt"));
        Assert.False(result.State.ExecutionAuthorized);
    }

    [Fact]
    public void CompletedRunHasDurableAuditRecord()
    {
        Assert.True(Run(new FileBinder(_root), Approve).Success);
        var records = Path.Combine(_root, ".upa", "runs");
        Assert.True(Directory.Exists(records));
        var record = Assert.Single(Directory.GetFiles(records, "*.json"));
        Assert.Contains("Completed", File.ReadAllText(record));
    }

    [Fact]
    public void NoOpExecutorCannotReportCompletion()
    {
        var binder = new FileBinder(_root) { SkipWrite = true };
        var result = Run(binder, Approve);
        Assert.False(result.Success);
        Assert.True(binder.RolledBack);
        Assert.DoesNotContain(result.State.Events, entry => entry.Stage == PipelineStage.Completed);
    }

    [Fact]
    public void LyingExecutorEvidenceCannotProveMissingOutput()
    {
        var binder = new FileBinder(_root) { SkipWrite = true, LieAboutVerification = true };
        Assert.False(Run(binder, Approve).Success);
    }

    [Fact]
    public void StaleApprovalCannotExecute()
    {
        var binder = new FileBinder(_root);
        Assert.False(Run(binder, preview => new ApprovalToken(preview.PlanId, "tester",
            DateTimeOffset.UtcNow.AddDays(-1), true) { ContentHash = preview.ContentHash }).Success);
        Assert.False(binder.Executed);
    }

    [Fact]
    public async Task ConcurrentRunsHaveSeparateAuditRecords()
    {
        var secondRoot = Path.Combine(_root, "second");
        Directory.CreateDirectory(secondRoot);
        var pipeline = new GovernedPipeline();
        using var barrier = new Barrier(2);
        ApprovalToken SynchronizedApproval(ExecutionPreview preview)
        {
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Concurrent test did not synchronize.");
            return Approve(preview);
        }
        var first = Task.Run(() => pipeline.Execute("first", _root, "Create a script.", new FileBinder(_root), SynchronizedApproval));
        var second = Task.Run(() => pipeline.Execute("second", secondRoot, "Create a script.", new FileBinder(secondRoot), SynchronizedApproval));
        var results = await Task.WhenAll(first, second);
        foreach (var result in results)
        {
            Assert.True(result.Success, string.Join("; ", result.Findings));
            Assert.Single(result.State.Events, entry => entry.Stage == PipelineStage.Completed);
            Assert.Single(result.State.Events, entry => entry.Stage == PipelineStage.Execute);
        }
    }

    [Fact]
    public void ChangingPreviewAfterApprovalCannotExecute()
    {
        var binder = new FileBinder(_root);
        var result = Run(binder, preview => {
            binder.PreviewSuffix = " Also change something else.";
            return Approve(preview);
        });
        Assert.False(result.Success);
        Assert.False(binder.Executed);
    }

    [Fact]
    public void WrongOutputIsRejectedAndRolledBack()
    {
        var binder = new FileBinder(_root) { WrongContent = true };
        Assert.False(Run(binder, Approve).Success);
        Assert.True(binder.RolledBack);
        Assert.False(File.Exists(Path.Combine(_root, "proof.txt")));
    }

    [Fact]
    public void RejectedApprovalDoesNotExecute()
    {
        var binder = new FileBinder(_root);
        var result = Run(binder, _ => null);
        Assert.Equal(PipelineStage.AwaitApproval, result.State.Stage);
        Assert.False(binder.Executed);
        Assert.False(File.Exists(Path.Combine(_root, "proof.txt")));
    }

    [Fact]
    public void ApprovalForAnotherPlanDoesNotExecute()
    {
        var binder = new FileBinder(_root);
        Assert.False(Run(binder, _ => new ApprovalToken("other", "tester", DateTimeOffset.UtcNow, true)).Success);
        Assert.False(binder.Executed);
    }

    [Theory]
    [InlineData("Do not create a script.")]
    [InlineData("Do the thing.")]
    public void InvalidIntentNeverReachesBinding(string intent)
    {
        var binder = new FileBinder(_root);
        var result = new GovernedPipeline().Execute("run", _root, intent, binder, Approve);
        Assert.False(result.Success);
        Assert.False(binder.Bound);
    }

    private PipelineResult Run(FileBinder binder, Func<ExecutionPreview, ApprovalToken?> approval)
        => new GovernedPipeline().Execute("run", _root, "Create a script.", binder, approval);

    private static ApprovalToken Approve(ExecutionPreview preview)
        => new(preview.PlanId, "test", DateTimeOffset.UtcNow, true) { ContentHash = preview.ContentHash };

    public void Dispose() => Directory.Delete(_root, true);

    private sealed class FileBinder(string root) : IPlanBinder, IVerifiedTransaction
    {
        public bool SkipWrite { get; init; }
        public bool WrongContent { get; init; }
        public bool LieAboutVerification { get; init; }
        public string PreviewSuffix { get; set; } = "";
        public bool Bound { get; private set; }
        public bool Executed { get; private set; }
        public bool RolledBack { get; private set; }
        private string FilePath => Path.Combine(root, "proof.txt");
        public string Preview => "Create proof.txt with content proof." + PreviewSuffix;
        public IReadOnlyList<OutputExpectation> ExpectedOutputs => new[] { new OutputExpectation("proof.txt", "text", "proof") };
        public IVerifiedTransaction Bind(UpaPlan plan, ScanResult scan)
        {
            Bound = true;
            return this;
        }
        public void CheckPreconditions()
        {
            if (File.Exists(FilePath)) throw new InvalidOperationException("Target already exists.");
        }
        public void Execute(ApprovalToken approval)
        {
            Executed = true;
            if (!SkipWrite) File.WriteAllText(FilePath, WrongContent ? "wrong" : "proof");
        }
        public IReadOnlyList<string> VerifyOutput()
        {
            if (LieAboutVerification) return new[] { "Everything verified, trust me." };
            if (!File.Exists(FilePath) || File.ReadAllText(FilePath) != "proof")
                throw new InvalidOperationException("Expected file content not found.");
            return new[] { "Read back proof.txt: proof" };
        }
        public void Rollback()
        {
            File.Delete(FilePath);
            RolledBack = true;
        }
    }
}
