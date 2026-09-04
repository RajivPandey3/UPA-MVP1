using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UPA.TrustLayer.Client.Models;

namespace UPA.TrustLayer.Client;

public interface ITrustLayerClient
{
    Task<IReadOnlyList<CertificateChainEntry>> EmitTrustAsync(
        string runId,
        string artifactBundleId,
        string artifactHash,
        string finalizedAuditSnapshot,
        CancellationToken cancellationToken = default);

    Task<TrustVerifyResponse> VerifyTrustAsync(
        string artifactBundleId,
        string artifactHash,
        IReadOnlyList<CertificateChainEntry> certificateChain,
        CancellationToken cancellationToken = default);

    Task<TrustInspectResponse> InspectTrustAsync(
        string entryId,
        CancellationToken cancellationToken = default);
}
