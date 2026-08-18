namespace UPA.ProjectModel;

public sealed record ScannerProvenance(
    string ScannerName,
    string ScannerVersion,
    DateTimeOffset CompletedAtUtc,
    bool ReadOnly);

public sealed record ProjectSnapshotManifest(
    string SchemaVersion,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ScannerProvenance> Sources);
