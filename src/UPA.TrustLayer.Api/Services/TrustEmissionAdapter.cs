using UPA.MVP3.TrustEmission;
using UPA.TrustLayer.Api.Contracts;

namespace UPA.TrustLayer.Api.Services;

public sealed class TrustEmissionAdapter : ITrustEmissionAdapter
{
    private readonly TrustEmitter _trustEmitter;

    public TrustEmissionAdapter(TrustEmitter trustEmitter)
    {
        _trustEmitter = trustEmitter;
    }

    public async Task<object> EmitAsync(
        TrustEmitRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var coreRequest = new TrustEmissionRequest(
            request.RunId,
            request.ArtifactBundleId,
            request.ArtifactHash,
            request.FinalizedAuditSnapshot
        );

        return await _trustEmitter.EmitAsync(coreRequest);
    }
}
