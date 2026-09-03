using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UPA.TrustLayer.Api.Contracts;

public sealed record TrustVerifyResponse
{
    [JsonPropertyName("valid")]
    public required bool Valid { get; init; }

    [JsonPropertyName("errors")]
    public required IReadOnlyList<string> Errors { get; init; }
}
