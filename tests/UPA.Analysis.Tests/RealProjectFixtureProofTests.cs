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
}
