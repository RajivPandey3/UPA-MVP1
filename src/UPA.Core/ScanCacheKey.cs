using System.Security.Cryptography;
using System.Text;

namespace UPA.Core;

public static class ScanCacheKey
{
    public static string Create(string projectRoot, string fixtureFingerprint)
    {
        if (string.IsNullOrWhiteSpace(projectRoot)) throw new ArgumentException("Project root is required.", nameof(projectRoot));
        if (string.IsNullOrWhiteSpace(fixtureFingerprint)) throw new ArgumentException("Fixture fingerprint is required.", nameof(fixtureFingerprint));
        var canonical = $"{Path.GetFullPath(projectRoot)}|{fixtureFingerprint}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
