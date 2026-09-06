using System.Security.Cryptography;

namespace UPA.Core;

public sealed record ReleaseEvidenceFile(string Path, string Sha256, long Length);

public static class ReleaseEvidenceManifest
{
    public static ReleaseEvidenceFile FromBytes(string path, ReadOnlySpan<byte> content)
    {
        var hash = Convert.ToHexString(SHA256.HashData(content));
        return Create(path, hash, content.Length);
    }

    public static ReleaseEvidenceFile Create(string path, string sha256, long length)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
        if (sha256.Length != 64 || sha256.Any(character => !Uri.IsHexDigit(character)))
            throw new ArgumentException("SHA-256 must be 64 hexadecimal characters.", nameof(sha256));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        return new ReleaseEvidenceFile(path, sha256.ToUpperInvariant(), length);
    }

    public static string Fingerprint(IEnumerable<ReleaseEvidenceFile> files)
    {
        var canonical = string.Join("\n", files.OrderBy(file => file.Path, StringComparer.Ordinal)
            .Select(file => $"{file.Path}|{file.Length}|{file.Sha256}"));
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical)));
    }
}
