using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UPA.TrustLayer.Api.Contracts;

public sealed record TrustEmitRequest
{
    [JsonPropertyName("run_id")]
    public required string RunId { get; init; }

    [JsonPropertyName("artifact_bundle_id")]
    public required string ArtifactBundleId { get; init; }

    [JsonPropertyName("artifact_hash")]
    public required string ArtifactHash { get; init; }

    [JsonPropertyName("finalized_audit_snapshot")]
    public required string FinalizedAuditSnapshot { get; init; }

    [JsonPropertyName("certificate_chain")]
    public required IReadOnlyList<CertificateChainEntry> CertificateChain { get; init; }
}

public sealed record CertificateChainEntry
{
    [JsonPropertyName("entry_id")]
    public required string EntryId { get; init; }

    [JsonPropertyName("bundle_id")]
    public required string BundleId { get; init; }

    [JsonPropertyName("bundle_fingerprint")]
    public required string BundleFingerprint { get; init; }

    [JsonPropertyName("sequence")]
    public required long Sequence { get; init; }

    [JsonPropertyName("registry_certificate_id")]
    public required string RegistryCertificateId { get; init; }

    [JsonPropertyName("registry_certificate_hash")]
    public required string RegistryCertificateHash { get; init; }

    [JsonPropertyName("registry_certificate_fingerprint")]
    public required string RegistryCertificateFingerprint { get; init; }

    [JsonPropertyName("previous_registry_certificate_id")]
    public string? PreviousRegistryCertificateId { get; init; }

    [JsonPropertyName("previous_registry_certificate_hash")]
    public string? PreviousRegistryCertificateHash { get; init; }

    [JsonPropertyName("certified_utc")]
    public required DateTimeOffset CertifiedUtc { get; init; }
}
