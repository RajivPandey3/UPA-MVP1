using System;
using System.Threading;
using System.Threading.Tasks;
using UPA.TrustLayer.Api.Contracts;

namespace UPA.TrustLayer.Api.Services;

public sealed class NotImplementedTrustInspectionAdapter : ITrustInspectionAdapter
{
    public Task<TrustInspectResponse> InspectAsync(
        string id,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Inspection adapter is not implemented yet.");
    }
}
