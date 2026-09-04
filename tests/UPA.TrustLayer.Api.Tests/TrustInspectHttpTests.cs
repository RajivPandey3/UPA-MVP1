using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace UPA.TrustLayer.Api.Tests;

public sealed class TrustInspectHttpTests
: IClassFixture<WebApplicationFactory<Program>>
{
private readonly HttpClient _client;

public TrustInspectHttpTests(
    WebApplicationFactory<Program> factory)
{
    _client = factory.CreateClient();
}

[Fact]
public async Task Inspect_ReturnsEmittedTrustByEntryId()
{
    var request = new
    {
        run_id = "inspect-http-run-" + System.Guid.NewGuid().ToString("N"),
        artifact_bundle_id = "inspect-http-bundle-" + System.Guid.NewGuid().ToString("N"),
        artifact_hash = "sha256:inspect-http-artifact",
        finalized_audit_snapshot = "inspect-http-audit",
        certificate_chain = System.Array.Empty<object>()
    };

    using var emitResponse =
        await _client.PostAsJsonAsync(
            "/v1/trust/emit",
            request);

    Assert.Equal(HttpStatusCode.OK, emitResponse.StatusCode);

    var emitBody =
        await emitResponse.Content.ReadFromJsonAsync<JsonElement>();

    var entryId = emitBody
        .GetProperty("entry_id")
        .GetString();

    Assert.False(string.IsNullOrWhiteSpace(entryId));

    using var inspectResponse =
        await _client.GetAsync(
            $"/v1/trust/{entryId}");

    Assert.Equal(HttpStatusCode.OK, inspectResponse.StatusCode);

    var inspectBody =
        await inspectResponse.Content.ReadFromJsonAsync<JsonElement>();

    Assert.Equal(entryId, inspectBody.GetProperty("id").GetString());
    Assert.Equal("emitted", inspectBody.GetProperty("status").GetString());

    var chain = inspectBody
        .GetProperty("certificate_chain")
        .EnumerateArray()
        .ToArray();

    Assert.Single(chain);
    Assert.Equal(
        entryId,
        chain[0].GetProperty("entry_id").GetString());
}

[Fact]
public async Task Inspect_ReturnsNotFoundForUnknownEntryId()
{
    var testId =
        "missing-inspect-id-" +
        System.Guid.NewGuid().ToString("N");

    using var response =
        await _client.GetAsync(
            $"/v1/trust/{testId}");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

    var body =
        await response.Content.ReadFromJsonAsync<
            System.Collections.Generic.Dictionary<string, JsonElement>>();

    Assert.NotNull(body);
    Assert.True(body.ContainsKey("code"));
    Assert.True(body.ContainsKey("message"));
    Assert.Equal(
        "TRUST_NOT_FOUND",
        body["code"].GetString());
}

}
