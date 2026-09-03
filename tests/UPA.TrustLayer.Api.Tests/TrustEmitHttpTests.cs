using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using UPA.TrustLayer.Api.Contracts;
using Xunit;

namespace UPA.TrustLayer.Api.Tests;

public sealed class TrustEmitHttpTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TrustEmitHttpTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Emit_ReturnsSuccessForValidRequest()
    {
        var request = new
        {
            run_id = "http-run-test",
            artifact_bundle_id = "http-bundle-test",
            artifact_hash = "http-hash-test",
            finalized_audit_snapshot = "http-opaque-snapshot",
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
                "/v1/trust/emit",
                request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<
                Dictionary<string, object>>();

        Assert.NotNull(body);
        Assert.NotEmpty(body!);
    }

    [Fact]
    public async Task Emit_RejectsMissingRequiredBodyFields()
    {
        var request = new
        {
            run_id = "http-run-invalid"
        };

        using var response =
            await _client.PostAsJsonAsync(
                "/v1/trust/emit",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}
