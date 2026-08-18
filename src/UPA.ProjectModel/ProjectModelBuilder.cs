namespace UPA.ProjectModel;

public sealed class ProjectModelBuilder
{
    private readonly List<ProjectFact> _facts = new();

    public ProjectModelBuilder AddFact(
        string key, string value, string source)
    {
        _facts.Add(new ProjectFact(key, value, source));
        return this;
    }

    public UnifiedProjectModel Build(
        string projectId,
        string projectName,
        string rootPath,
        string? unityVersion,
        string? renderPipeline,
        ProjectCounts counts)
    {
        var facts = _facts
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ThenBy(x => x.Source, StringComparer.Ordinal)
            .ToArray();

        return new UnifiedProjectModel(
            projectId,
            projectName,
            rootPath,
            unityVersion,
            renderPipeline,
            counts,
            facts,
            DateTimeOffset.UtcNow,
            "1.0");
    }
}
