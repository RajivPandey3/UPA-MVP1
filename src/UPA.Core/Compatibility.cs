namespace UPA.Core;

public enum CompatibilityStatus
{
    Verified,
    Partial,
    Unsupported,
    Unknown
}

public sealed record CompatibilityEntry(string AdapterId, string HostVersion, CompatibilityStatus Status, string Evidence);

public sealed class CompatibilityMatrix
{
    private readonly Dictionary<(string Adapter, string Version), CompatibilityEntry> entries = new();

    public void Add(CompatibilityEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (string.IsNullOrWhiteSpace(entry.AdapterId) || string.IsNullOrWhiteSpace(entry.HostVersion))
            throw new ArgumentException("Adapter and host version are required.");
        entries[(entry.AdapterId, entry.HostVersion)] = entry;
    }

    public CompatibilityEntry Resolve(string adapterId, string hostVersion) =>
        entries.TryGetValue((adapterId, hostVersion), out var entry)
            ? entry
            : new CompatibilityEntry(adapterId, hostVersion, CompatibilityStatus.Unknown, "No verification evidence.");
}
