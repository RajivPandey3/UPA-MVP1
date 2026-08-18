namespace UPA.ProjectModel;

public sealed record ProjectFact(
    string Key,
    string Value,
    string Source,
    bool IsFact = true);

public sealed record ProjectCounts(
    int Scripts,
    int Types,
    int Assemblies,
    int Scenes,
    int GameObjects,
    int Prefabs,
    int Assets,
    int References,
    int UnresolvedReferences,
    int Diagnostics);

public sealed record UnifiedProjectModel(
    string ProjectId,
    string ProjectName,
    string RootPath,
    string? UnityVersion,
    string? RenderPipeline,
    ProjectCounts Counts,
    IReadOnlyList<ProjectFact> Facts,
    DateTimeOffset SnapshotTimeUtc,
    string SchemaVersion);
