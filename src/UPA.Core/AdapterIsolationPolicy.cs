namespace UPA.Core;

public sealed record AdapterIsolationPolicy(string AllowedRoot, bool NetworkAccess = false)
{
    public bool AllowsPath(string path)
    {
        var root = Path.GetFullPath(AllowedRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(path);
        return candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }
}
