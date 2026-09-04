using System.Threading;
using System.Threading.Tasks;
using UPA.TrustLayer.Api.Contracts;

namespace UPA.TrustLayer.Api.Services;

public sealed class TrustInspectionAdapter : ITrustInspectionAdapter
{
private readonly ITrustInspectionService _inspectionService;


public TrustInspectionAdapter(ITrustInspectionService inspectionService)
{
    _inspectionService = inspectionService;
}

public async Task<TrustInspectResponse> InspectAsync(
    string id,
    CancellationToken cancellationToken)
{
    var entry = await _inspectionService.FindByEntryIdAsync(
        id,
        cancellationToken);

    if (entry is null)
    {
        throw new TrustInspectionNotFoundException(id);
    }

    return new TrustInspectResponse
    {
        Id = entry.EntryId,
        Status = "emitted",
        CertificateChain = new[] { new CertificateChainEntry
        {
            EntryId = entry.EntryId,
            BundleId = entry.BundleId,
            BundleFingerprint = entry.BundleFingerprint,
            Sequence = entry.Sequence,
            RegistryCertificateId = entry.RegistryCertificateId,
            RegistryCertificateHash = entry.RegistryCertificateHash,
            RegistryCertificateFingerprint = entry.RegistryCertificateFingerprint,
            PreviousRegistryCertificateId = entry.PreviousRegistryCertificateId,
            PreviousRegistryCertificateHash = entry.PreviousRegistryCertificateHash,
            CertifiedUtc = entry.CertifiedUtc
        }}
    };
}


}

public sealed class TrustInspectionNotFoundException : Exception
{
public TrustInspectionNotFoundException(string entryId)
: base($"Trust entry '{entryId}' was not found.")
{
}
}

