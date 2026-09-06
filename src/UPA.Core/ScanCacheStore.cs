namespace UPA.Core;

public sealed class ScanCacheStore
{
    public void Save(string directory, string key, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(payload);
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".{key}.tmp");
        var target = Path.Combine(directory, $"{key}.cache");
        File.WriteAllText(temporary, payload);
        File.Move(temporary, target, true);
    }

    public string? Load(string directory, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var path = Path.Combine(directory, $"{key}.cache");
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    public bool Invalidate(string directory, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var path = Path.Combine(directory, $"{key}.cache");
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }
}
