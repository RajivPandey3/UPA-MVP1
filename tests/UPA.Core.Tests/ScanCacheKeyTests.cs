using Xunit;

namespace UPA.Core.Tests;

public sealed class ScanCacheKeyTests
{
    [Fact]
    public void Create_IsStableForSameProjectAndFingerprint()
    {
        Assert.Equal(ScanCacheKey.Create(".", "fixture-a"), ScanCacheKey.Create(".", "fixture-a"));
    }

    [Fact]
    public void Create_ChangesWhenFingerprintChanges()
    {
        Assert.NotEqual(ScanCacheKey.Create(".", "fixture-a"), ScanCacheKey.Create(".", "fixture-b"));
    }
}
