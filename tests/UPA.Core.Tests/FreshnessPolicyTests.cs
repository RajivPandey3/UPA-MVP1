using System;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class FreshnessPolicyTests
{
    [Fact]
    public void ClassifiesFreshStaleAndFutureEvidence()
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        Assert.Equal(EvidenceStatus.Confirmed, FreshnessPolicy.Classify(now.AddMinutes(-5), now, TimeSpan.FromMinutes(10)));
        Assert.Equal(EvidenceStatus.Stale, FreshnessPolicy.Classify(now.AddMinutes(-11), now, TimeSpan.FromMinutes(10)));
        Assert.Equal(EvidenceStatus.Conflicted, FreshnessPolicy.Classify(now.AddMinutes(1), now, TimeSpan.FromMinutes(10)));
    }
}
