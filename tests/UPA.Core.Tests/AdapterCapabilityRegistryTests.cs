using System;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class AdapterCapabilityRegistryTests
{
    [Fact]
    public void FailsClosedForUnknownCapability()
    {
        var registry = new AdapterCapabilityRegistry();
        Assert.Throws<NotSupportedException>(() => registry.EnsureExecutable("web", "deploy"));
        registry.Register(new AdapterCapability("web", "deploy", CompatibilityStatus.Verified, "ci-proof"));
        registry.EnsureExecutable("web", "deploy");
    }
}
