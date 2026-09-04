using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UPA.TrustLayer.Api.Contracts;
using Xunit;

namespace UPA.TrustLayer.Api.Tests
{
    public class TrustConcurrencyHttpTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _options;

        public TrustConcurrencyHttpTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
            _options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        }

        [Fact]
        public async Task EmitAsync_HighConcurrency_SameBundle_EnforcesCollisionSafety()
        {
            int parallelRequests = 50;
            var tasks = new Task<HttpResponseMessage>[parallelRequests];

            for (int i = 0; i < parallelRequests; i++)
            {
                var payload = new
                {
                    run_id = $"run-concurrent-{i}",
                    artifact_bundle_id = "bundle-concurrent-test",
                    artifact_hash = "hash-123",
                    finalized_audit_snapshot = "snapshot",
                    certificate_chain = Array.Empty<object>()
                };

                tasks[i] = _client.PostAsJsonAsync("/v1/trust/emit", payload, _options);
            }

            var responses = await Task.WhenAll(tasks);

            int successCount = 0;
            int conflictCount = 0;

            foreach (var response in responses)
            {
                if (response.IsSuccessStatusCode)
                {
                    successCount++;
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var error = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>(_options);
                    if (error != null && error.TryGetPropertyValue("code", out var codeNode) && codeNode?.GetValue<string>() == "BUNDLE_COLLISION")
                    {
                        conflictCount++;
                    }
                }
                else
                {
                    Assert.Fail($"Unexpected status code: {response.StatusCode}");
                }
            }

            Assert.Equal(1, successCount); // Exactly one succeeds
            Assert.Equal(parallelRequests - 1, conflictCount); // The rest fail with collision
        }

        [Fact]
        public async Task EmitAsync_HighConcurrency_SameRunId_EnforcesIdempotencySafety()
        {
            int parallelRequests = 50;
            var tasks = new Task<HttpResponseMessage>[parallelRequests];

            var payload = new
            {
                run_id = "run-idempotent-test",
                artifact_bundle_id = "bundle-idempotent-test",
                artifact_hash = "hash-456",
                finalized_audit_snapshot = "snapshot",
                certificate_chain = Array.Empty<object>()
            };

            for (int i = 0; i < parallelRequests; i++)
            {
                tasks[i] = _client.PostAsJsonAsync("/v1/trust/emit", payload, _options);
            }

            var responses = await Task.WhenAll(tasks);

            int successCount = 0;
            foreach (var response in responses)
            {
                if (response.IsSuccessStatusCode)
                {
                    successCount++;
                }
                else
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    Assert.Fail($"Unexpected status code: {response.StatusCode} Body: {msg}");
                }
            }

            // All should succeed because idempotency returns the exact same cached entry
            Assert.Equal(parallelRequests, successCount);
        }
    }
}
