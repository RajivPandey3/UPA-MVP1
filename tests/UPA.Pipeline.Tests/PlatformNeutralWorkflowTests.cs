using UPA.Core;
using UPA.Execution;
using UPA.Pipeline;
using UPA.Planning;

namespace UPA.Pipeline.Tests;

public sealed class PlatformNeutralWorkflowTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("upa-platform-").FullName;

    [Fact]
    public void WorkflowRunsWithoutUnitySpecificTypes()
    {
        var adapter = new TextPlatformAdapter(_root);
        var result = new WorkflowRunner().Execute("web-run", _root, "Create a text artifact.", adapter,
            preview => new ApprovalToken(preview.PlanId, "test", DateTimeOffset.UtcNow, true)
            {
                ContentHash = preview.ContentHash
            });
        Assert.True(result.Success, string.Join("; ", result.Findings));
        Assert.Equal("hello", File.ReadAllText(Path.Combine(_root, "artifact.txt")));
    }

    public void Dispose() => Directory.Delete(_root, true);

    private sealed class TextPlatformAdapter(string root) : IPlatformAdapter
    {
        public string Id => "text-platform";
        public string Version => "1.0";
        public IReadOnlyList<string> Capabilities { get; } = new[] { "create-text" };
        public PreparedWorkflow Prepare(string projectRoot, string intent)
        {
            var plan = new WorkflowPlan("text-plan", Id, Version, "create-text", intent);
            return new PreparedWorkflow(plan, new Transaction(root));
        }

        private sealed class Transaction(string root) : IVerifiedTransaction
        {
            public string Preview => "Create artifact.txt containing hello.";
            public IReadOnlyList<OutputExpectation> ExpectedOutputs => new[] { new OutputExpectation("artifact.txt", "text", "hello") };
            public void CheckPreconditions() { }
            public void Execute(ApprovalToken approval) => File.WriteAllText(Path.Combine(root, "artifact.txt"), "hello");
            public IReadOnlyList<string> VerifyOutput() => new[] { "adapter returned" };
            public void Rollback() => File.Delete(Path.Combine(root, "artifact.txt"));
        }
    }
}
