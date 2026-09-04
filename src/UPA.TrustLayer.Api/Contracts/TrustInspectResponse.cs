using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UPA.TrustLayer.Api.Contracts;

public sealed record TrustInspectResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("certificate_chain")]
    public required IReadOnlyList<CertificateChainEntry> CertificateChain { get; init; }
}
