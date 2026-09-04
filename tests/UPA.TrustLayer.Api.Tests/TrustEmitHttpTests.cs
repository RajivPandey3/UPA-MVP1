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
                Dictionary<string, System.Text.Json.JsonElement>>();

        Assert.NotNull(body);
        Assert.NotEmpty(body!);

        Assert.True(body.ContainsKey("entry_id"));
        Assert.True(body.ContainsKey("bundle_id"));
        Assert.True(body.ContainsKey("bundle_fingerprint"));
        Assert.True(body.ContainsKey("sequence"));
        Assert.True(body.ContainsKey("registry_certificate_id"));
        Assert.True(body.ContainsKey("registry_certificate_hash"));
        Assert.True(body.ContainsKey("registry_certificate_fingerprint"));
        Assert.True(body.ContainsKey("previous_registry_certificate_id"));
        Assert.True(body.ContainsKey("previous_registry_certificate_hash"));
        Assert.True(body.ContainsKey("certified_utc"));

        Assert.Equal("http-bundle-test", body["bundle_id"].GetString());
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

    [Fact]
    public async Task Emit_ReturnsIdempotencyConflictCodeForConflictingRunId()
    {
        var firstRequest = new
        {
            run_id = "http-idempotency-run",
            artifact_bundle_id = "http-idempotency-bundle",
            artifact_hash = "http-idempotency-hash",
            finalized_audit_snapshot = "http-idempotency-snapshot",
            certificate_chain = Array.Empty<object>()
        };

        using var firstResponse =
            await _client.PostAsJsonAsync(
                "/v1/trust/emit",
                firstRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var conflictingRequest = new
        {
            run_id = "http-idempotency-run",
            artifact_bundle_id = "http-idempotency-bundle",
            artifact_hash = "different-artifact-hash",
            finalized_audit_snapshot = "http-idempotency-snapshot",
            certificate_chain = Array.Empty<object>()
        };

        using var response =
            await _client.PostAsJsonAsync(
                "/v1/trust/emit",
                conflictingRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<
                Dictionary<string, System.Text.Json.JsonElement>>();

        Assert.NotNull(body);
        Assert.Equal(
            "IDEMPOTENCY_CONFLICT",
            body!["code"].GetString());
    }

    [Fact]
    public async Task Emit_ReturnsBundleCollisionCodeForConflictingBundle()
    {
        var firstRequest = new
        {
            run_id = "http-collision-run-1",
            artifact_bundle_id = "http-collision-bundle",
            artifact_hash = "http-collision-hash-1",
            finalized_audit_snapshot = "http-collision-snapshot-1",
            certificate_chain = Array.Empty<object>()
        };

        using var firstResponse =
            await _client.PostAsJsonAsync(
                "/v1/trust/emit",
                firstRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var conflictingRequest = new
        {
            run_id = "http-collision-run-2",
            artifact_bundle_id = "http-collision-bundle",
            artifact_hash = "http-collision-hash-2",
            finalized_audit_snapshot = "http-collision-snapshot-2",
            certificate_chain = Array.Empty<object>()
        };

        using var response =
            await _client.PostAsJsonAsync(
                "/v1/trust/emit",
                conflictingRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var body =
            await response.Content.ReadFromJsonAsync<
                Dictionary<string, System.Text.Json.JsonElement>>();

        Assert.NotNull(body);
        Assert.Equal(
            "BUNDLE_COLLISION",
            body!["code"].GetString());
    }
}
