using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using UPA.TrustLayer.Api.Contracts;
using Xunit;

namespace UPA.TrustLayer.Api.Tests;

public sealed class TrustVerifyHttpTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TrustVerifyHttpTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Verify_ReturnsNotImplementedForValidRequest()
    {
        var request = new
        {
            artifact_bundle_id = "http-bundle-test",
            artifact_hash = "http-hash-test",
            certificate_chain = new[]
            {
                new
                {
                    entry_id = "entry-test",
                    bundle_id = "http-bundle-test",
                    bundle_fingerprint = "fingerprint-test",
                    sequence = 1,
                    registry_certificate_id = "registry-cert-test",
                    registry_certificate_hash = "registry-hash-test",
                    registry_certificate_fingerprint = "registry-fingerprint-test",
                    previous_registry_certificate_id = (string?)null,
                    previous_registry_certificate_hash = (string?)null,
                    certified_utc = "2026-01-01T00:00:00Z"
                }
            }
        };

        using var response =
            await _client.PostAsJsonAsync(
                "/v1/trust/verify",
                request);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<
            System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>>();

        Assert.NotNull(body);
        Assert.True(body.ContainsKey("code"));
        Assert.True(body.ContainsKey("message"));
        Assert.Equal("TRUST_VERIFY_NOT_IMPLEMENTED", body["code"].GetString());
    }

    [Fact]
    public async Task Verify_RejectsMissingRequiredBodyFields()
    {
        var request = new
        {
            artifact_bundle_id = "http-bundle-invalid"
        };

        using var response =
            await _client.PostAsJsonAsync(
                "/v1/trust/verify",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}
