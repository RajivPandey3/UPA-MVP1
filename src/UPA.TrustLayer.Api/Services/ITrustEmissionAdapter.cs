using UPA.TrustLayer.Api.Contracts;

namespace UPA.TrustLayer.Api.Services;

/// <summary>
/// Adapter boundary between the V1.1 HTTP contract and the frozen
/// V1.0 TrustEmission implementation.
/// </summary>
public interface ITrustEmissionAdapter
{
    Task<UPA.VerificationTrustAnchor.CertificateChainEntry> EmitAsync(
        TrustEmitRequest request,
        CancellationToken cancellationToken);
}
