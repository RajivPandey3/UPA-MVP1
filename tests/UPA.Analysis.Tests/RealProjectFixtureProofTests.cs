using UPA.Core;
using Xunit;

namespace UPA.Analysis.Tests;

public sealed class RealProjectFixtureProofTests
{
    [Fact]
    public void RealDotnetFixture_ScansAndProjectsProjectNode()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../fixtures/real-project-dotnet"));
        var scan = new ProjectScanner().Scan(new ScanContext(root));
        var graph = ScanKnowledgeProjector.ProjectGraph(scan);

        Assert.Equal("real-project-dotnet", scan.ProjectName);
        Assert.Equal(root, scan.ProjectRoot);
        Assert.Contains(graph.Nodes, node => node.Dimension == KnowledgeDimension.Project);
    }

    [Fact]
    public void RealDotnetFixture_MutationAndRestoreReconcileDeterministically()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../fixtures/real-project-dotnet"));
        var path = Path.Combine(root, "Program.cs");
        var original = File.ReadAllText(path);
        try
        {
            var before = Node(path, original);
            File.AppendAllText(path, "\n// mutation");
            var changed = Node(path, File.ReadAllText(path));
            Assert.Equal(ReconciliationChangeKind.Changed,
                Assert.Single(ReconciliationEngine.Compare(new[] { before }, new[] { changed })).Kind);
            File.WriteAllText(path, original);
            var restored = Node(path, File.ReadAllText(path));
            Assert.Equal(ReconciliationChangeKind.Unchanged,
                Assert.Single(ReconciliationEngine.Compare(new[] { before }, new[] { restored })).Kind);
        }
        finally { File.WriteAllText(path, original); }
    }

    [Fact]
    public async Task RealDotnetFixture_CancelledScanFailsFast()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../fixtures/real-project-dotnet"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new ProjectScanner().ScanAsync(new ScanContext(root), cancellation.Token));
    }

    [Fact]
    public async Task RealDotnetFixture_CanResumeAfterCancellation()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../fixtures/real-project-dotnet"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new ProjectScanner().ScanAsync(new ScanContext(root), cancellation.Token));

        var resumed = await new ProjectScanner().ScanAsync(new ScanContext(root));
        Assert.Equal("real-project-dotnet", resumed.ProjectName);
        Assert.Empty(resumed.Diagnostics);
    }

    [Fact]
    public void RealDotnetFixture_RepeatedScansHaveStableIdentity()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../fixtures/real-project-dotnet"));
        var scanner = new ProjectScanner();
        var first = scanner.Scan(new ScanContext(root));
        var second = scanner.Scan(new ScanContext(root));

        Assert.Equal(first.ProjectId, second.ProjectId);
        Assert.Equal(first.ProjectName, second.ProjectName);
        Assert.Equal(first.ProjectRoot, second.ProjectRoot);
    }

    private static ProjectKnowledgeNode Node(string path, string content)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content)));
        return new ProjectKnowledgeNode(EntityId.FromStableKey(path), KnowledgeDimension.Hierarchy,
            path, $"SourceFile:{hash}", path, EvidenceStatus.Confirmed, DateTimeOffset.UtcNow);
    }
}
