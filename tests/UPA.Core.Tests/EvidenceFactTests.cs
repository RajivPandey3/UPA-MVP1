using System;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class EvidenceFactTests
{
    [Fact]
    public void AcceptsBoundedEvidenceWithExplicitStatus()
    {
        var fact = new EvidenceFact(EntityId.FromStableKey("project:demo"), "contains", "Assets", EvidenceStatus.Confirmed, 1m, DateTimeOffset.UtcNow, "scanner");
        Assert.Equal(EvidenceStatus.Confirmed, fact.Status);
        Assert.Equal(1m, fact.Confidence);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void RejectsInvalidConfidence(double confidence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EvidenceFact(EntityId.New(), "contains", "x", EvidenceStatus.Inferred, (decimal)confidence, DateTimeOffset.UtcNow, "test"));
    }
}
