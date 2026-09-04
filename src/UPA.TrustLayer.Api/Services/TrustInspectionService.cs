using System.Text.Json;
using UPA.MVP3.TrustEmission;
using UPA.VerificationTrustAnchor;

namespace UPA.TrustLayer.Api.Services;

public interface ITrustInspectionService
{
Task<CertificateChainEntry?> FindByEntryIdAsync(
string entryId,
CancellationToken cancellationToken);
}

public sealed class TrustInspectionService : ITrustInspectionService
{
private readonly string _stateFilePath;


public TrustInspectionService(IConfiguration configuration)
{
    _stateFilePath = configuration["TrustEmission:StateFilePath"]
        ?? throw new InvalidOperationException(
            "TrustEmission:StateFilePath configuration is required.");
}

public Task<CertificateChainEntry?> FindByEntryIdAsync(
    string entryId,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    if (string.IsNullOrWhiteSpace(entryId))
    {
        return Task.FromResult<CertificateChainEntry?>(null);
    }

    if (!File.Exists(_stateFilePath))
    {
        return Task.FromResult<CertificateChainEntry?>(null);
    }

    var json = File.ReadAllText(_stateFilePath);
    var state = JsonSerializer.Deserialize<DurableState>(json);

    if (state is null)
    {
        return Task.FromResult<CertificateChainEntry?>(null);
    }

    var entry = state.ProcessedRuns.Values
        .Select(record => record.Entry)
        .FirstOrDefault(entry => entry.EntryId == entryId);

    return Task.FromResult(entry);
}


}

