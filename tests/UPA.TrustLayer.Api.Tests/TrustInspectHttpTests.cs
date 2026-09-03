using System.Net;
using System.Net.Http.Json;
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
    public async Task Inspect_ReturnsNotImplementedForAnyId()
    {
        var testId = "test-inspect-id";

        using var response =
            await _client.GetAsync(
                $"/v1/trust/{testId}");

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<
            System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>>();

        Assert.NotNull(body);
        Assert.True(body.ContainsKey("code"));
        Assert.True(body.ContainsKey("message"));
        Assert.Equal("TRUST_INSPECT_NOT_IMPLEMENTED", body["code"].GetString());
    }
}
