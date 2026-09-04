using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using UPA.TrustLayer.Api.Contracts;
using UPA.TrustLayer.Api.Services;

namespace UPA.TrustLayer.Mcp.Tools;

public class EmitTrustTool
{
    private readonly ITrustEmissionAdapter _adapter;

    public EmitTrustTool(ITrustEmissionAdapter adapter)
    {
        _adapter = adapter;
    }

    [McpServerTool(Name = "emit_trust")]
    [System.ComponentModel.Description("Emits a new trust certificate chain for a given artifact bundle.")]
    public async Task<IReadOnlyList<CertificateChainEntry>> ExecuteAsync(
        string run_id,
        string artifact_bundle_id,
        string artifact_hash,
        string finalized_audit_snapshot,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(run_id)) throw new ArgumentException("run_id is required", nameof(run_id));
        if (string.IsNullOrWhiteSpace(artifact_bundle_id)) throw new ArgumentException("artifact_bundle_id is required", nameof(artifact_bundle_id));
        if (string.IsNullOrWhiteSpace(artifact_hash)) throw new ArgumentException("artifact_hash is required", nameof(artifact_hash));
        if (string.IsNullOrWhiteSpace(finalized_audit_snapshot)) throw new ArgumentException("finalized_audit_snapshot is required", nameof(finalized_audit_snapshot));

        var request = new TrustEmitRequest
        {
            RunId = run_id,
            ArtifactBundleId = artifact_bundle_id,
            ArtifactHash = artifact_hash,
            FinalizedAuditSnapshot = finalized_audit_snapshot,
            CertificateChain = Array.Empty<CertificateChainEntry>()
        };

        try
        {
            var coreEntry = await _adapter.EmitAsync(request, cancellationToken);

            var mappedDto = new CertificateChainEntry
            {
                EntryId = coreEntry.EntryId,
                BundleId = coreEntry.BundleId,
                BundleFingerprint = coreEntry.BundleFingerprint,
                Sequence = coreEntry.Sequence,
                RegistryCertificateId = coreEntry.RegistryCertificateId,
                RegistryCertificateHash = coreEntry.RegistryCertificateHash,
                RegistryCertificateFingerprint = coreEntry.RegistryCertificateFingerprint,
                PreviousRegistryCertificateId = coreEntry.PreviousRegistryCertificateId,
                PreviousRegistryCertificateHash = coreEntry.PreviousRegistryCertificateHash,
                CertifiedUtc = coreEntry.CertifiedUtc
            };

            return new List<CertificateChainEntry> { mappedDto };
        }
        catch (Exception ex) when (
            ex.GetType().Name == "IdempotencyConflictException" ||
            ex.GetType().Name == "BundleCollisionException")
        {
            string code = ex.GetType().Name == "IdempotencyConflictException" ? "IDEMPOTENCY_CONFLICT" : "BUNDLE_COLLISION";
            throw new InvalidOperationException($"[{code}] {ex.Message}", ex);
        }
    }
}
