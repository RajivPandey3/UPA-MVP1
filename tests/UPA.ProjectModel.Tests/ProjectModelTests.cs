using UPA.ProjectModel;

namespace UPA.ProjectModel.Tests;

public class ProjectModelTests
{
    [Fact]
    public void Builder_ProducesDeterministicFactOrdering()
    {
        var builder = new ProjectModelBuilder()
            .AddFact("unity.version", "6000.0.0f1", "ProjectScanner")
            .AddFact("render.pipeline", "URP", "ProjectScanner");

        var model = builder.Build(
            "project-1",
            "Demo",
            "/Demo",
            "6000.0.0f1",
            "URP",
            new ProjectCounts(10, 12, 2, 3, 40, 5, 100, 30, 1, 0));

        Assert.Equal("render.pipeline", model.Facts[0].Key);
        Assert.Equal("unity.version", model.Facts[1].Key);
        Assert.Empty(ProjectModelIntegrity.Validate(model));
    }

    [Fact]
    public void Integrity_RejectsImpossibleReferenceCounts()
    {
        var model = new UnifiedProjectModel(
            "p", "Demo", "/Demo", null, null,
            new ProjectCounts(0,0,0,0,0,0,0,1,2,0),
            Array.Empty<ProjectFact>(),
            DateTimeOffset.UtcNow,
            "1.0");

        Assert.Contains(
            ProjectModelIntegrity.Validate(model),
            x => x.Code == "MODEL-004");
    }
}
