using System.Net;
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
    }
}
