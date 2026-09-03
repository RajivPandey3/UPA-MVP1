using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UPA.TrustLayer.Api.Contracts;

namespace UPA.TrustLayer.Api.Services;

public sealed class TrustVerificationAdapter : ITrustVerificationAdapter
{
    private readonly ITrustVerificationService _verificationService;

    public TrustVerificationAdapter(ITrustVerificationService verificationService)
    {
        _verificationService = verificationService;
    }

    public async Task<TrustVerifyResponse> VerifyAsync(
        TrustVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var coreEntries = request.CertificateChain.Select(dto => new UPA.VerificationTrustAnchor.CertificateChainEntry(
            dto.EntryId,
            dto.BundleId,
            dto.BundleFingerprint,
            dto.Sequence,
            dto.RegistryCertificateId,
            dto.RegistryCertificateHash,
            dto.RegistryCertificateFingerprint,
            dto.PreviousRegistryCertificateId,
            dto.PreviousRegistryCertificateHash,
            dto.CertifiedUtc
        )).ToList();

        var result = await _verificationService.VerifyAsync(
            request.ArtifactBundleId,
            request.ArtifactHash,
            coreEntries,
            cancellationToken);

        return new TrustVerifyResponse
        {
            Valid = result.Valid,
            Errors = result.Errors.ToList()
        };
    }
}
