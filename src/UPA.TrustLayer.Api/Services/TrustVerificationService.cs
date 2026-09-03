using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UPA.VerificationTrustAnchor;

namespace UPA.TrustLayer.Api.Services;

public sealed class TrustVerificationService : ITrustVerificationService
{
    public Task<TrustVerificationResult> VerifyAsync(
        string artifactBundleId,
        string artifactHash,
        IReadOnlyList<CertificateChainEntry> entries,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<string>();

        if (entries == null || entries.Count == 0)
        {
            errors.Add("Certificate chain is empty");
            return Task.FromResult(new TrustVerificationResult(false, errors));
        }

        var firstEntry = entries[0];

        if (artifactBundleId != firstEntry.BundleId)
        {
            errors.Add("Artifact bundle ID mismatch");
        }

        if (artifactHash != firstEntry.BundleFingerprint)
        {
            errors.Add("Artifact hash mismatch");
        }

        var chain = new RegistryCertificateChain();
        foreach (var entry in entries)
        {
            try
            {
                chain.Register(entry);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add($"Registration error: {ex.Message}");
            }
        }

        var intrinsicResult = chain.Verify();
        if (!intrinsicResult.Valid)
        {
            errors.AddRange(intrinsicResult.Errors);
        }

        bool isValid = errors.Count == 0;
        return Task.FromResult(new TrustVerificationResult(isValid, errors));
    }
}
