#pragma warning disable CS1998, CS8602
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using UPA.TrustLayer.Client;
using UPA.TrustLayer.Client.Exceptions;
using UPA.TrustLayer.Client.Models;
using UPA.TrustLayer.Client.Tests.Helpers;
using Xunit;

namespace UPA.TrustLayer.Client.Tests;

public class TrustLayerClientTests
{
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [Fact]
    public async Task EmitTrustAsync_Success_ReturnsChain()
    {
        var expectedChain = new List<CertificateChainEntry>
        {
            new CertificateChainEntry
            {
                EntryId = "e1",
                BundleId = "b1",
                BundleFingerprint = "f1",
                Sequence = 1,
                RegistryCertificateId = "r1",
                RegistryCertificateHash = "h1",
                RegistryCertificateFingerprint = "rf1",
                CertifiedUtc = DateTimeOffset.UtcNow
            }
        };

        var handler = new MockHttpMessageHandler(async req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("http://localhost/v1/trust/emit", req.RequestUri.ToString());
            var content = await req.Content!.ReadAsStringAsync();
            Assert.Contains("run_id", content!);
            Assert.DoesNotContain("certificate_chain", content!);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedChain, _options))
            };
        });

        var client = new TrustLayerClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var result = await client.EmitTrustAsync("run1", "b1", "h1", "snap1");

        Assert.Single(result);
        Assert.Equal("e1", result[0].EntryId);
    }

    [Fact]
    public async Task EmitTrustAsync_OpaqueSnapshot_TransmitsUnchanged()
    {
        var jsonSnapshot = "{ \"opaque\": true, \"data\": [1, 2, 3] }";
        var handler = new MockHttpMessageHandler(async req =>
        {
            var content = await req.Content!.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content!);
            var snapshot = jsonDoc.RootElement.GetProperty("finalized_audit_snapshot").GetString();
            Assert.Equal(jsonSnapshot, snapshot);
            
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            };
        });

        var client = new TrustLayerClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        await client.EmitTrustAsync("run1", "b1", "h1", jsonSnapshot);
    }

    [Fact]
    public async Task EmitTrustAsync_IdempotencyConflict_ThrowsMappedException()
    {
        var errorResp = new TrustErrorResponse { Code = "IDEMPOTENCY_CONFLICT", Message = "Conflict" };
        var handler = new MockHttpMessageHandler(async req =>
        {
            return new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(JsonSerializer.Serialize(errorResp, _options))
            };
        });

        var client = new TrustLayerClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        var ex = await Assert.ThrowsAsync<TrustIdempotencyConflictException>(() => client.EmitTrustAsync("r1", "b1", "h1", "s1"));
        Assert.Equal("Conflict", ex.Message);
        Assert.Equal("IDEMPOTENCY_CONFLICT", ex.ErrorCode);
        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task EmitTrustAsync_BundleCollision_ThrowsMappedException()
    {
        var errorResp = new TrustErrorResponse { Code = "BUNDLE_COLLISION", Message = "Collision" };
        var handler = new MockHttpMessageHandler(async req =>
        {
            return new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(JsonSerializer.Serialize(errorResp, _options))
            };
        });

        var client = new TrustLayerClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });

        await Assert.ThrowsAsync<TrustBundleCollisionException>(() => client.EmitTrustAsync("r1", "b1", "h1", "s1"));
    }

    [Theory]
    [InlineData(null, "b1", "h1", "s1")]
    [InlineData("r1", " ", "h1", "s1")]
    [InlineData("r1", "b1", "", "s1")]
    [InlineData("r1", "b1", "h1", null)]
    public async Task EmitTrustAsync_InvalidInputs_ThrowsArgumentException(string r, string b, string h, string s)
    {
        var client = new TrustLayerClient(new HttpClient());
        await Assert.ThrowsAsync<ArgumentException>(() => client.EmitTrustAsync(r, b, h, s));
    }

    [Fact]
    public async Task VerifyTrustAsync_ValidResponse_ReturnsResult()
    {
        var expectedResponse = new TrustVerifyResponse { Valid = true, Errors = Array.Empty<string>() };
        var handler = new MockHttpMessageHandler(async req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Equal("http://localhost/v1/trust/verify", req.RequestUri.ToString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse, _options))
            };
        });

        var client = new TrustLayerClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        var result = await client.VerifyTrustAsync("b1", "h1", Array.Empty<CertificateChainEntry>());

        Assert.True(result.Valid);
    }

    [Fact]
    public async Task VerifyTrustAsync_InvalidInputs_ThrowsArgumentException()
    {
        var client = new TrustLayerClient(new HttpClient());
        await Assert.ThrowsAsync<ArgumentException>(() => client.VerifyTrustAsync(null!, "h1", Array.Empty<CertificateChainEntry>()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.VerifyTrustAsync("b1", "h1", null!));
    }

    [Fact]
    public async Task InspectTrustAsync_Found_ReturnsStatus()
    {
        var expectedResponse = new TrustInspectResponse { Id = "e1", Status = "emitted", CertificateChain = Array.Empty<CertificateChainEntry>() };
        var handler = new MockHttpMessageHandler(async req =>
        {
            Assert.Equal(HttpMethod.Get, req.Method);
            Assert.Equal("http://localhost/v1/trust/e1", req.RequestUri.ToString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse, _options))
            };
        });

        var client = new TrustLayerClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        var result = await client.InspectTrustAsync("e1");

        Assert.Equal("emitted", result.Status);
    }

    [Fact]
    public async Task InspectTrustAsync_NotFound_ThrowsMappedException()
    {
        var errorResp = new TrustErrorResponse { Code = "TRUST_NOT_FOUND", Message = "Missing" };
        var handler = new MockHttpMessageHandler(async req =>
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(JsonSerializer.Serialize(errorResp, _options))
            };
        });

        var client = new TrustLayerClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        await Assert.ThrowsAsync<TrustNotFoundException>(() => client.InspectTrustAsync("e1"));
    }

    [Fact]
    public async Task GeneralHttpError_ThrowsTrustLayerException()
    {
        var handler = new MockHttpMessageHandler(async req =>
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("some error")
            };
        });

        var client = new TrustLayerClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") });
        await Assert.ThrowsAsync<HttpRequestException>(() => client.InspectTrustAsync("e1"));
    }
}








