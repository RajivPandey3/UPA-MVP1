using System;

namespace UPA.MVP3.TrustEmission
{
    public sealed record TrustEmissionRequest(
        string RunId,
        string ArtifactBundleId,
        string ArtifactHash,
        string FinalizedAuditSnapshot
    );
}
