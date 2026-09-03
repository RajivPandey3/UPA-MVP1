using Microsoft.Extensions.Configuration;
using UPA.MVP3.TrustEmission;
using UPA.VerificationTrustAnchor;
using UPA.TrustLayer.Api.Contracts;
using UPA.TrustLayer.Api.Services;
using Xunit;

namespace UPA.TrustLayer.Api.Tests;

public sealed class TrustEmissionAdapterTests
{
    [Fact]
    public void Factory_UsesConfiguredStatePath()
    {
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["TrustEmission:StateFilePath"] =
                            "test-state.json"
                    })
                .Build();

        var emitter =
            TrustEmitterFactory.Create(configuration);

        Assert.NotNull(emitter);
    }

    [Fact]
    public async Task Adapter_MapsApprovedContractFields()
    {
        var emitter =
            new TrustEmitter(
                "adapter-test-state.json",
                new RegistryCertificateChain());

        var adapter =
            new TrustEmissionAdapter(emitter);

        var request = new TrustEmitRequest
        {
            RunId = "run-test",
            ArtifactBundleId = "bundle-test",
            ArtifactHash = "hash-test",
            FinalizedAuditSnapshot = "opaque-snapshot",
            CertificateChain =
            [
                new UPA.TrustLayer.Api.Contracts.CertificateChainEntry
                {
                    EntryId = "entry-test",
                    BundleId = "bundle-test",
                    BundleFingerprint = "fingerprint-test",
                    Sequence = 1,
                    RegistryCertificateId = "registry-cert-test",
                    RegistryCertificateHash = "registry-hash-test",
                    RegistryCertificateFingerprint =
                        "registry-fingerprint-test",
                    PreviousRegistryCertificateId = null,
                    PreviousRegistryCertificateHash = null,
                    CertifiedUtc =
                        DateTimeOffset.Parse(
                            "2026-01-01T00:00:00Z")
                }
            ]
        };

        var result =
            await adapter.EmitAsync(
                request,
                CancellationToken.None);

        Assert.NotNull(result);
    }
}
