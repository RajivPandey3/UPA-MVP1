using System.Text.Json.Serialization;

namespace UPA.TrustLayer.Api.Contracts;

public sealed record TrustErrorResponse
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("request_id")]
    public string? RequestId { get; init; }
}
