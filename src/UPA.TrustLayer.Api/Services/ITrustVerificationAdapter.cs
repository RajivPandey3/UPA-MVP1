using System.Threading;
using System.Threading.Tasks;
using UPA.TrustLayer.Api.Contracts;

namespace UPA.TrustLayer.Api.Services;

/// <summary>
/// Adapter boundary for V1.1 HTTP verification contract.
/// </summary>
public interface ITrustVerificationAdapter
{
    Task<TrustVerifyResponse> VerifyAsync(
        TrustVerifyRequest request,
        CancellationToken cancellationToken);
}
