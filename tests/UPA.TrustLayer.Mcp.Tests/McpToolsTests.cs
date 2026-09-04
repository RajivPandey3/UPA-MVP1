using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using UPA.TrustLayer.Api.Contracts;
using UPA.TrustLayer.Api.Services;
using UPA.TrustLayer.Mcp.Tools;
using Xunit;
using UPA.VerificationTrustAnchor;

namespace UPA.TrustLayer.Mcp.Tests;
using UPA.MVP3.TrustEmission;

public class McpToolsTests
{
    [Fact]
    public async Task EmitTrustTool_Success_ReturnsMappedChain()
    {
        var mockAdapter = new Mock<ITrustEmissionAdapter>();
        var entry = new UPA.VerificationTrustAnchor.CertificateChainEntry(
            "e1", "b1", "f1", 1, "r1", "h1", "rf1", null, null, DateTimeOffset.UtcNow
        );

        mockAdapter.Setup(a => a.EmitAsync(It.IsAny<TrustEmitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entry);

        var tool = new EmitTrustTool(mockAdapter.Object);
        var result = await tool.ExecuteAsync("run1", "b1", "f1", "snap1", CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("e1", result[0].EntryId);
    }

    [Theory]
    [InlineData(null, "b1", "f1", "snap1")]
    [InlineData("run1", " ", "f1", "snap1")]
    [InlineData("run1", "b1", "", "snap1")]
    [InlineData("run1", "b1", "f1", null)]
    public async Task EmitTrustTool_MissingInput_ThrowsArgumentException(
        string runId, string bundleId, string hash, string snapshot)
    {
        var mockAdapter = new Mock<ITrustEmissionAdapter>();
        var tool = new EmitTrustTool(mockAdapter.Object);
        await Assert.ThrowsAsync<ArgumentException>(() => 
            tool.ExecuteAsync(runId, bundleId, hash, snapshot, CancellationToken.None));
    }

    [Fact]
    public async Task EmitTrustTool_IdempotencyConflict_ThrowsMappedException()
    {
        var mockAdapter = new Mock<ITrustEmissionAdapter>();
        mockAdapter.Setup(a => a.EmitAsync(It.IsAny<TrustEmitRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IdempotencyConflictException("conflict"));

        var tool = new EmitTrustTool(mockAdapter.Object);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tool.ExecuteAsync("run1", "b1", "f1", "snap1", CancellationToken.None));
        Assert.Contains("[IDEMPOTENCY_CONFLICT]", ex.Message);
    }

    [Fact]
    public async Task EmitTrustTool_BundleCollision_ThrowsMappedException()
    {
        var mockAdapter = new Mock<ITrustEmissionAdapter>();
        mockAdapter.Setup(a => a.EmitAsync(It.IsAny<TrustEmitRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BundleCollisionException("collision"));

        var tool = new EmitTrustTool(mockAdapter.Object);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tool.ExecuteAsync("run1", "b1", "f1", "snap1", CancellationToken.None));
        Assert.Contains("[BUNDLE_COLLISION]", ex.Message);
    }

    [Fact]
    public async Task VerifyTrustTool_Valid_ReturnsTrue()
    {
        var mockAdapter = new Mock<ITrustVerificationAdapter>();
        var response = new TrustVerifyResponse { Valid = true, Errors = Array.Empty<string>() };
        
        mockAdapter.Setup(a => a.VerifyAsync(It.IsAny<TrustVerifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var tool = new VerifyTrustTool(mockAdapter.Object);
        var result = await tool.ExecuteAsync("b1", "f1", Array.Empty<Api.Contracts.CertificateChainEntry>(), CancellationToken.None);

        Assert.True(result.Valid);
    }

    [Theory]
    [InlineData(null, "f1")]
    [InlineData("b1", "")]
    public async Task VerifyTrustTool_MissingStringInput_ThrowsArgumentException(string bundleId, string hash)
    {
        var mockAdapter = new Mock<ITrustVerificationAdapter>();
        var tool = new VerifyTrustTool(mockAdapter.Object);
        await Assert.ThrowsAsync<ArgumentException>(() => 
            tool.ExecuteAsync(bundleId, hash, Array.Empty<Api.Contracts.CertificateChainEntry>(), CancellationToken.None));
    }

    [Fact]
    public async Task VerifyTrustTool_MissingChain_ThrowsArgumentNullException()
    {
        var mockAdapter = new Mock<ITrustVerificationAdapter>();
        var tool = new VerifyTrustTool(mockAdapter.Object);
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            tool.ExecuteAsync("b1", "f1", null!, CancellationToken.None));
    }

    [Fact]
    public async Task VerifyTrustTool_Invalid_ReturnsFalseWithErrors()
    {
        var mockAdapter = new Mock<ITrustVerificationAdapter>();
        var response = new TrustVerifyResponse { Valid = false, Errors = new[] { "invalid identity" } };
        
        mockAdapter.Setup(a => a.VerifyAsync(It.IsAny<TrustVerifyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var tool = new VerifyTrustTool(mockAdapter.Object);
        var result = await tool.ExecuteAsync("b1", "f1", Array.Empty<Api.Contracts.CertificateChainEntry>(), CancellationToken.None);

        Assert.False(result.Valid);
        Assert.Contains("invalid identity", result.Errors);
    }

    [Fact]
    public async Task InspectTrustTool_Found_ReturnsEmittedStatus()
    {
        var mockAdapter = new Mock<ITrustInspectionAdapter>();
        var response = new TrustInspectResponse { Id = "e1", Status = "emitted", CertificateChain = Array.Empty<Api.Contracts.CertificateChainEntry>() };
        
        mockAdapter.Setup(a => a.InspectAsync("e1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var tool = new InspectTrustTool(mockAdapter.Object);
        var result = await tool.ExecuteAsync("e1", CancellationToken.None);

        Assert.Equal("emitted", result.Status);
        Assert.Equal("e1", result.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("")]
    public async Task InspectTrustTool_MissingInput_ThrowsArgumentException(string id)
    {
        var mockAdapter = new Mock<ITrustInspectionAdapter>();
        var tool = new InspectTrustTool(mockAdapter.Object);
        await Assert.ThrowsAsync<ArgumentException>(() => 
            tool.ExecuteAsync(id, CancellationToken.None));
    }

    [Fact]
    public async Task InspectTrustTool_NotFound_ThrowsMappedException()
    {
        var mockAdapter = new Mock<ITrustInspectionAdapter>();
        mockAdapter.Setup(a => a.InspectAsync("e1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TrustInspectionNotFoundException("missing"));

        var tool = new InspectTrustTool(mockAdapter.Object);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => tool.ExecuteAsync("e1", CancellationToken.None));
        
        Assert.Contains("[TRUST_NOT_FOUND]", ex.Message);
    }
}
