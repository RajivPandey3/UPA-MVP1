using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UPA.TrustLayer.Api.Contracts;

public sealed record TrustVerifyRequest
{
    [JsonPropertyName("artifact_bundle_id")]
    public required string ArtifactBundleId { get; init; }

    [JsonPropertyName("artifact_hash")]
    public required string ArtifactHash { get; init; }

    [JsonPropertyName("certificate_chain")]
    public required IReadOnlyList<CertificateChainEntry> CertificateChain { get; init; }
}
