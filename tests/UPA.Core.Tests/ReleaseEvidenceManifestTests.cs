using Xunit;

namespace UPA.Core.Tests;

public sealed class ReleaseEvidenceManifestTests
{
    [Fact]
    public void FromBytes_ProducesDeterministicSha256AndLength()
    {
        var file = ReleaseEvidenceManifest.FromBytes("evidence.json", "proof"u8);

        Assert.Equal("evidence.json", file.Path);
        Assert.Equal(5, file.Length);
        Assert.Equal(64, file.Sha256.Length);
    }

    [Fact]
    public void Fingerprint_IsIndependentOfInputOrder()
    {
        var first = ReleaseEvidenceManifest.FromBytes("b", "two"u8);
        var second = ReleaseEvidenceManifest.FromBytes("a", "one"u8);

        Assert.Equal(ReleaseEvidenceManifest.Fingerprint(new[] { first, second }),
            ReleaseEvidenceManifest.Fingerprint(new[] { second, first }));
    }
}
