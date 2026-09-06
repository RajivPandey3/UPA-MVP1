using System.Security.Cryptography;

namespace UPA.Core;

public sealed record ReleaseEvidenceFile(string Path, string Sha256, long Length);

public static class ReleaseEvidenceManifest
{
    public static ReleaseEvidenceFile FromBytes(string path, ReadOnlySpan<byte> content)
    {
        var hash = Convert.ToHexString(SHA256.HashData(content));
        return new ReleaseEvidenceFile(path, hash, content.Length);
    }

    public static string Fingerprint(IEnumerable<ReleaseEvidenceFile> files)
    {
        var canonical = string.Join("\n", files.OrderBy(file => file.Path, StringComparer.Ordinal)
            .Select(file => $"{file.Path}|{file.Length}|{file.Sha256}"));
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical)));
    }
}
