using System.IO;
using Xunit;

namespace UPA.Core.Tests;

public sealed class AdapterIsolationPolicyTests
{
    [Fact]
    public void AllowsOnlyPathsInsideRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "upa-adapter");
        var policy = new AdapterIsolationPolicy(root);
        Assert.True(policy.AllowsPath(Path.Combine(root, "project", "file.cs")));
        Assert.False(policy.AllowsPath(Path.Combine(root, "..", "outside.cs")));
        Assert.False(policy.NetworkAccess);
    }
}
