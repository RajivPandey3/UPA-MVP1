namespace UPA.ProjectModel;

public sealed record IntegrityIssue(
    string Code,
    string Severity,
    string Message);

public static class ProjectModelIntegrity
{
    public static IReadOnlyList<IntegrityIssue> Validate(
        UnifiedProjectModel model)
    {
        var issues = new List<IntegrityIssue>();

        if (string.IsNullOrWhiteSpace(model.ProjectId))
            issues.Add(new("MODEL-001", "Error", "ProjectId is empty."));

        if (string.IsNullOrWhiteSpace(model.ProjectName))
            issues.Add(new("MODEL-002", "Error", "ProjectName is empty."));

        if (string.IsNullOrWhiteSpace(model.RootPath))
            issues.Add(new("MODEL-003", "Error", "RootPath is empty."));

        if (model.Counts.UnresolvedReferences > model.Counts.References)
            issues.Add(new(
                "MODEL-004",
                "Error",
                "Unresolved reference count exceeds total references."));

        if (model.Counts.Scripts < 0 ||
            model.Counts.Types < 0 ||
            model.Counts.Assemblies < 0 ||
            model.Counts.Scenes < 0 ||
            model.Counts.GameObjects < 0 ||
            model.Counts.Prefabs < 0 ||
            model.Counts.Assets < 0 ||
            model.Counts.References < 0 ||
            model.Counts.UnresolvedReferences < 0)
        {
            issues.Add(new(
                "MODEL-005",
                "Error",
                "Project counts cannot be negative."));
        }

        return issues;
    }
}
