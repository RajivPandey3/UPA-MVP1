using UPA.Core;

namespace UPA.Core.Tests;

public sealed class CompatibilityMatrixTests
{
    [Fact]
    public void ReturnsUnknownUntilEvidenceIsRegistered()
    {
        var matrix = new CompatibilityMatrix();
        Assert.Equal(CompatibilityStatus.Unknown, matrix.Resolve("unity", "6000.0").Status);
        matrix.Add(new CompatibilityEntry("unity", "6000.0", CompatibilityStatus.Verified, "ci-run-1"));
        Assert.Equal(CompatibilityStatus.Verified, matrix.Resolve("unity", "6000.0").Status);
    }
}
