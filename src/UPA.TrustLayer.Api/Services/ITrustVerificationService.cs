using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UPA.VerificationTrustAnchor;

namespace UPA.TrustLayer.Api.Services;

public interface ITrustVerificationService
{
    Task<TrustVerificationResult> VerifyAsync(
        string artifactBundleId,
        string artifactHash,
        IReadOnlyList<CertificateChainEntry> entries,
        CancellationToken cancellationToken);
}
