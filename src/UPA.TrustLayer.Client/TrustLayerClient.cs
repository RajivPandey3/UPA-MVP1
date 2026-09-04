using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using UPA.TrustLayer.Client.Exceptions;
using UPA.TrustLayer.Client.Models;

namespace UPA.TrustLayer.Client;

public class TrustLayerClient : ITrustLayerClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public TrustLayerClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    public async Task<IReadOnlyList<CertificateChainEntry>> EmitTrustAsync(
        string runId,
        string artifactBundleId,
        string artifactHash,
        string finalizedAuditSnapshot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("runId is required", nameof(runId));
        if (string.IsNullOrWhiteSpace(artifactBundleId)) throw new ArgumentException("artifactBundleId is required", nameof(artifactBundleId));
        if (string.IsNullOrWhiteSpace(artifactHash)) throw new ArgumentException("artifactHash is required", nameof(artifactHash));
        if (string.IsNullOrWhiteSpace(finalizedAuditSnapshot)) throw new ArgumentException("finalizedAuditSnapshot is required", nameof(finalizedAuditSnapshot));

        var request = new TrustEmitRequest
        {
            RunId = runId,
            ArtifactBundleId = artifactBundleId,
            ArtifactHash = artifactHash,
            FinalizedAuditSnapshot = finalizedAuditSnapshot
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/trust/emit", request, _jsonOptions, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<CertificateChainEntry>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return result ?? Array.Empty<CertificateChainEntry>();
    }

    public async Task<TrustVerifyResponse> VerifyTrustAsync(
        string artifactBundleId,
        string artifactHash,
        IReadOnlyList<CertificateChainEntry> certificateChain,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artifactBundleId)) throw new ArgumentException("artifactBundleId is required", nameof(artifactBundleId));
        if (string.IsNullOrWhiteSpace(artifactHash)) throw new ArgumentException("artifactHash is required", nameof(artifactHash));
        if (certificateChain == null) throw new ArgumentNullException(nameof(certificateChain));

        var request = new TrustVerifyRequest
        {
            ArtifactBundleId = artifactBundleId,
            ArtifactHash = artifactHash,
            CertificateChain = certificateChain
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/trust/verify", request, _jsonOptions, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<TrustVerifyResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return result ?? throw new TrustLayerException("Failed to deserialize verify response.");
    }

    public async Task<TrustInspectResponse> InspectTrustAsync(
        string entryId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entryId)) throw new ArgumentException("entryId is required", nameof(entryId));

        var url = $"/v1/trust/{Uri.EscapeDataString(entryId)}";
        var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<TrustInspectResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return result ?? throw new TrustLayerException("Failed to deserialize inspect response.");
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        TrustErrorResponse? errorDto = null;
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(content))
            {
                errorDto = JsonSerializer.Deserialize<TrustErrorResponse>(content, _jsonOptions);
            }
        }
        catch
        {
            // Ignore deserialization errors, fallback to generic exception
        }

        if (errorDto != null)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                if (errorDto.Code == "IDEMPOTENCY_CONFLICT")
                    throw new TrustIdempotencyConflictException(errorDto.Message);
                if (errorDto.Code == "BUNDLE_COLLISION")
                    throw new TrustBundleCollisionException(errorDto.Message);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                if (errorDto.Code == "TRUST_NOT_FOUND")
                    throw new TrustNotFoundException(errorDto.Message);
            }

            throw new TrustLayerException(errorDto.Message, response.StatusCode, errorDto.Code);
        }

        response.EnsureSuccessStatusCode();
    }
}
