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

    private static ProjectKnowledgeNode Node(string path, string content)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content)));
        return new ProjectKnowledgeNode(EntityId.FromStableKey(path), KnowledgeDimension.Hierarchy,
            path, $"SourceFile:{hash}", path, EvidenceStatus.Confirmed, DateTimeOffset.UtcNow);
    }
}
