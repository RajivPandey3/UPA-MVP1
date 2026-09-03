using UPA.MVP3.TrustEmission;
using UPA.VerificationTrustAnchor;

namespace UPA.TrustLayer.Api.Services;

/// <summary>
/// Creates the frozen V1.0 TrustEmitter using API configuration.
/// The V1.0 constructor itself is not modified.
/// </summary>
public static class TrustEmitterFactory
{
    public static TrustEmitter Create(
        IConfiguration configuration)
    {
        var stateFilePath =
            configuration["TrustEmission:StateFilePath"];

        if (string.IsNullOrWhiteSpace(stateFilePath))
        {
            throw new InvalidOperationException(
                "TrustEmission:StateFilePath configuration is required.");
        }

        // Phase 2D uses the proven V1.0 construction pattern:
        // new TrustEmitter(stateFilePath, new RegistryCertificateChain()).
        //
        // No V1.0 core source is modified here.
        return new TrustEmitter(
            stateFilePath,
            new RegistryCertificateChain());
    }
}
