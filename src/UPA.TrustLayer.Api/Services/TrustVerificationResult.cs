using System.Collections.Generic;

namespace UPA.TrustLayer.Api.Services;

public sealed record TrustVerificationResult(
    bool Valid,
    IReadOnlyList<string> Errors);
