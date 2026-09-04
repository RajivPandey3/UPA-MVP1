using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using UPA.TrustLayer.Api.Contracts;
using UPA.TrustLayer.Api.Services;

namespace UPA.TrustLayer.Mcp.Tools;

public class VerifyTrustTool
{
    private readonly ITrustVerificationAdapter _adapter;

    public VerifyTrustTool(ITrustVerificationAdapter adapter)
    {
        _adapter = adapter;
    }

    [McpServerTool(Name = "verify_trust")]
    [System.ComponentModel.Description("Verifies the continuity and identity of an existing certificate chain against an artifact.")]
    public async Task<TrustVerifyResponse> ExecuteAsync(
        string artifact_bundle_id,
        string artifact_hash,
        IReadOnlyList<CertificateChainEntry> certificate_chain,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(artifact_bundle_id)) throw new ArgumentException("artifact_bundle_id is required", nameof(artifact_bundle_id));
        if (string.IsNullOrWhiteSpace(artifact_hash)) throw new ArgumentException("artifact_hash is required", nameof(artifact_hash));
        if (certificate_chain == null) throw new ArgumentNullException(nameof(certificate_chain), "certificate_chain is required");

        var request = new TrustVerifyRequest
        {
            ArtifactBundleId = artifact_bundle_id,
            ArtifactHash = artifact_hash,
            CertificateChain = certificate_chain
        };

        var response = await _adapter.VerifyAsync(request, cancellationToken);
        return response;
    }
}
