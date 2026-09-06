namespace UPA.Core;

public sealed record AttachmentFile(string RelativePath, string Sha256);

public sealed record ProjectAttachmentManifest
{
    public ProjectAttachmentManifest(string manifestVersion, EntityId projectId, string adapterId,
        string adapterVersion, IReadOnlyList<string> permissions, IReadOnlyList<AttachmentFile> ownedFiles,
        DateTimeOffset installedAt)
    {
        if (string.IsNullOrWhiteSpace(manifestVersion)) throw new ArgumentException("Manifest version is required.", nameof(manifestVersion));
        if (string.IsNullOrWhiteSpace(adapterId)) throw new ArgumentException("Adapter id is required.", nameof(adapterId));
        if (string.IsNullOrWhiteSpace(adapterVersion)) throw new ArgumentException("Adapter version is required.", nameof(adapterVersion));
        if (permissions is null) throw new ArgumentNullException(nameof(permissions));
        if (ownedFiles is null) throw new ArgumentNullException(nameof(ownedFiles));
        ManifestVersion = manifestVersion; ProjectId = projectId; AdapterId = adapterId; AdapterVersion = adapterVersion;
        Permissions = permissions; OwnedFiles = ownedFiles; InstalledAt = installedAt;
    }
    public string ManifestVersion { get; }
    public EntityId ProjectId { get; }
    public string AdapterId { get; }
    public string AdapterVersion { get; }
    public IReadOnlyList<string> Permissions { get; }
    public IReadOnlyList<AttachmentFile> OwnedFiles { get; }
    public DateTimeOffset InstalledAt { get; }
}
