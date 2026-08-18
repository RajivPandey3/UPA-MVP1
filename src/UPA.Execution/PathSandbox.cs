namespace UPA.Execution;

public sealed class PathSandbox
{
    private readonly string _root;

    public PathSandbox(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    public string Resolve(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            throw new ArgumentException("Relative path is required.", nameof(relativePath));

        var full = Path.GetFullPath(Path.Combine(_root, relativePath));

        var rootWithSeparator =
            _root.TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        if (!full.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(full, _root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Path escapes the execution sandbox.");
        }

        return full;
    }
}
