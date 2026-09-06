using UPA.Core;

namespace UPA.Core.Tests;

public sealed class SecurityPolicyTests
{
    [Fact]
    public void BlocksHighAndCriticalFindings()
    {
        Assert.True(SecurityPolicy.BlocksRelease(new[] { new VulnerabilityFinding("CVE-1", VulnerabilitySeverity.High, "pkg", "scan") }));
        Assert.True(SecurityPolicy.BlocksRelease(new[] { new VulnerabilityFinding("CVE-2", VulnerabilitySeverity.Critical, "pkg", "scan") }));
    }

    [Fact]
    public void AllowsLowAndModerateForReview()
    {
        Assert.False(SecurityPolicy.BlocksRelease(new[] { new VulnerabilityFinding("CVE-3", VulnerabilitySeverity.Low, "pkg", "scan") }));
        Assert.False(SecurityPolicy.BlocksRelease(new[] { new VulnerabilityFinding("CVE-4", VulnerabilitySeverity.Moderate, "pkg", "scan") }));
    }
}
