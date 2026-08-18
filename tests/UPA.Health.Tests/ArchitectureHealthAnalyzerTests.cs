using UPA.Health;
using UPA.ProjectModel;

namespace UPA.Health.Tests;

public class ArchitectureHealthAnalyzerTests
{
    [Fact]
    public void AnalyzerFlagsUnresolvedReferences()
    {
        var model = new UnifiedProjectModel(
            "p", "Demo", "/Demo", "6000.0.0f1", "URP",
            new ProjectCounts(
                Scripts: 10, Types: 12, Assemblies: 2, Scenes: 3,
                GameObjects: 50, Prefabs: 8, Assets: 100,
                References: 20, UnresolvedReferences: 2, Diagnostics: 0),
            Array.Empty<ProjectFact>(),
            DateTimeOffset.UtcNow,
            "1.0");

        var report = new ArchitectureHealthAnalyzer().Analyze(model);

        Assert.Contains(report.Findings, x => x.Code == "HEALTH-REF-001");
        Assert.True(report.Score.Score < 100);
    }

    [Fact]
    public void AnalyzerProducesHealthyReportForCleanModel()
    {
        var model = new UnifiedProjectModel(
            "p", "Demo", "/Demo", "6000.0.0f1", "URP",
            new ProjectCounts(
                10, 12, 2, 3, 50, 8, 100, 20, 0, 0),
            Array.Empty<ProjectFact>(),
            DateTimeOffset.UtcNow,
            "1.0");

        var report = new ArchitectureHealthAnalyzer().Analyze(model);

        Assert.Empty(report.Findings);
        Assert.Equal("A+", report.Score.Grade);
        Assert.True(HealthGovernance.IsSafeForPlanning(report));
        Assert.False(HealthGovernance.IsSafeForAutofix(report));
    }

    [Fact]
    public void AnalyzerDoesNotGrantAutofixPermission()
    {
        var model = new UnifiedProjectModel(
            "p", "Demo", "/Demo", null, null,
            new ProjectCounts(0,0,0,0,0,0,0,0,0,0),
            Array.Empty<ProjectFact>(),
            DateTimeOffset.UtcNow,
            "1.0");

        var report = new ArchitectureHealthAnalyzer().Analyze(model);

        Assert.False(HealthGovernance.IsSafeForAutofix(report));
    }
}
