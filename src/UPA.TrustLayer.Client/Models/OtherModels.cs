using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UPA.TrustLayer.Client.Models;

public sealed record TrustVerifyRequest
{
    [JsonPropertyName("artifact_bundle_id")]
    public required string ArtifactBundleId { get; init; }

    [JsonPropertyName("artifact_hash")]
    public required string ArtifactHash { get; init; }

    [JsonPropertyName("certificate_chain")]
    public required IReadOnlyList<CertificateChainEntry> CertificateChain { get; init; }
}

public sealed record TrustVerifyResponse
{
    [JsonPropertyName("valid")]
    public required bool Valid { get; init; }

    [JsonPropertyName("errors")]
    public required IReadOnlyList<string> Errors { get; init; }
}

public sealed record TrustInspectResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("certificate_chain")]
    public required IReadOnlyList<CertificateChainEntry> CertificateChain { get; init; }
}

public sealed record TrustErrorResponse
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }
}
