using System;
using System.Threading;
using System.Threading.Tasks;
using UPA.TrustLayer.Api.Contracts;

namespace UPA.TrustLayer.Api.Services;

public sealed class NotImplementedTrustVerificationAdapter : ITrustVerificationAdapter
{
    public Task<TrustVerifyResponse> VerifyAsync(
        TrustVerifyRequest request,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Verification adapter is not implemented yet.");
    }
}
