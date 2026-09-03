using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UPA.TrustLayer.Api.Services;
using UPA.VerificationTrustAnchor;
using Xunit;

namespace UPA.TrustLayer.Api.Tests;

public sealed class TrustVerificationServiceTests
{
    private readonly TrustVerificationService _service = new();

    private CertificateChainEntry CreateEntry(
        string bundleId = "test-bundle",
        string bundleFp = "test-hash",
        long sequence = 1,
        string? prevId = null,
        string? prevHash = null,
        string entryId = "entry-1",
        string certId = "cert-1",
        string certHash = "cert-hash-1",
        string certFp = "cert-fp-1")
    {
        return new CertificateChainEntry(
            entryId, bundleId, bundleFp, sequence, certId, certHash, certFp, prevId, prevHash, DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task VerifyAsync_EmptyChain_ReturnsFalse()
    {
        var result = await _service.VerifyAsync("b1", "h1", Array.Empty<CertificateChainEntry>(), CancellationToken.None);

        Assert.False(result.Valid);
        Assert.Contains("Certificate chain is empty", result.Errors);
    }

    [Fact]
    public async Task VerifyAsync_BundleIdMismatch_ReturnsFalse()
    {
        var entries = new[] { CreateEntry(bundleId: "real-bundle") };
        var result = await _service.VerifyAsync("wrong-bundle", "test-hash", entries, CancellationToken.None);

        Assert.False(result.Valid);
        Assert.Contains("Artifact bundle ID mismatch", result.Errors);
    }

    [Fact]
    public async Task VerifyAsync_ArtifactHashMismatch_ReturnsFalse()
    {
        var entries = new[] { CreateEntry(bundleFp: "real-hash") };
        var result = await _service.VerifyAsync("test-bundle", "wrong-hash", entries, CancellationToken.None);

        Assert.False(result.Valid);
        Assert.Contains("Artifact hash mismatch", result.Errors);
    }

    [Fact]
    public async Task VerifyAsync_ValidContiguousChain_ReturnsTrue()
    {
        var entries = new[]
        {
            CreateEntry(entryId: "e1", sequence: 1, certId: "c1", certHash: "ch1", certFp: "cf1", prevId: "", prevHash: ""),
            CreateEntry(entryId: "e2", sequence: 2, certId: "c2", certHash: "ch2", certFp: "cf2", prevId: "c1", prevHash: "ch1")
        };

        var result = await _service.VerifyAsync("test-bundle", "test-hash", entries, CancellationToken.None);

        Assert.True(result.Valid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task VerifyAsync_BrokenIntrinsicChain_ReturnsFalseAndPreservesErrors()
    {
        var entries = new[]
        {
            CreateEntry(entryId: "e1", sequence: 1, certId: "c1", certHash: "ch1", certFp: "cf1", prevId: "", prevHash: ""),
            CreateEntry(entryId: "e2", sequence: 2, certId: "c2", certHash: "ch2", certFp: "cf2", prevId: "wrong", prevHash: "ch1")
        };

        var result = await _service.VerifyAsync("test-bundle", "test-hash", entries, CancellationToken.None);

        Assert.False(result.Valid);
        Assert.Contains("Previous certificate ID break: e2.", result.Errors);
    }

    [Fact]
    public async Task VerifyAsync_IdentityMismatchAndIntrinsicFailure_CombinesErrors()
    {
        var entries = new[]
        {
            CreateEntry(bundleId: "real-bundle", entryId: "e1", sequence: 1, certId: "c1", certHash: "ch1", certFp: "cf1", prevId: "", prevHash: ""),
            CreateEntry(bundleId: "real-bundle", entryId: "e2", sequence: 2, certId: "c2", certHash: "ch2", certFp: "cf2", prevId: "wrong", prevHash: "ch1")
        };

        var result = await _service.VerifyAsync("wrong-bundle", "test-hash", entries, CancellationToken.None);

        Assert.False(result.Valid);
        Assert.Contains("Artifact bundle ID mismatch", result.Errors);
        Assert.Contains("Previous certificate ID break: e2.", result.Errors);
    }
}
