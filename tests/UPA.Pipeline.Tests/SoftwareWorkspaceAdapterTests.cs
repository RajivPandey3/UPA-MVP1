using UPA.Execution;
using UPA.Pipeline;

namespace UPA.Pipeline.Tests;

public sealed class SoftwareWorkspaceAdapterTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("upa-workspace-").FullName;
    private readonly SoftwareWorkspaceAdapter adapter = new();

    [Fact]
    public void CreatesAndVerifiesTextFile()
    {
        var result = Run("Create text file \"docs/hello.txt\" with content \"hello\".");
        Assert.True(result.Success, string.Join("; ", result.Findings));
        Assert.Equal("hello", File.ReadAllText(Path.Combine(root, "docs", "hello.txt")));
    }

    [Fact]
    public void RequiresApprovalBeforeMutation()
    {
        var result = new WorkflowRunner().Execute("no-approval", root,
            "Create text file \"blocked.txt\" with content \"x\".", adapter, _ => null);
        Assert.False(result.Success);
        Assert.False(File.Exists(Path.Combine(root, "blocked.txt")));
    }

    [Fact]
    public void RejectsTraversalAndUnsupportedIntent()
    {
        Assert.Throws<InvalidOperationException>(() => adapter.Prepare(root, "Create text file \"../escape.txt\" with content \"x\"."));
        Assert.Throws<InvalidOperationException>(() => adapter.Prepare(root, "Run a shell command."));
    }

    [Fact]
    public void RefusesOverwrite()
    {
        var path = Path.Combine(root, "existing.txt");
        File.WriteAllText(path, "original");
        var result = Run("Create text file \"existing.txt\" with content \"new\".");
        Assert.False(result.Success);
        Assert.Equal("original", File.ReadAllText(path));
    }

    private PipelineResult Run(string intent) => new WorkflowRunner().Execute(
        Guid.NewGuid().ToString("N"), root, intent, adapter,
        preview => new ApprovalToken(preview.PlanId, "test", DateTimeOffset.UtcNow, true)
        {
            ContentHash = preview.ContentHash
        });

    public void Dispose() => Directory.Delete(root, true);
}
