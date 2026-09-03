using System.Threading;
using System.Threading.Tasks;
using UPA.TrustLayer.Api.Contracts;

namespace UPA.TrustLayer.Api.Services;

/// <summary>
/// Adapter boundary for V1.1 HTTP inspection contract.
/// </summary>
public interface ITrustInspectionAdapter
{
    Task<TrustInspectResponse> InspectAsync(
        string id,
        CancellationToken cancellationToken);
}
