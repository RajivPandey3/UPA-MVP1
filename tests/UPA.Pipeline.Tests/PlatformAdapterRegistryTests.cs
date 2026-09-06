using UPA.Pipeline;

namespace UPA.Pipeline.Tests;

public sealed class PlatformAdapterRegistryTests
{
    [Fact]
    public void ResolvesOnlyDeclaredCapability()
    {
        var registry = new PlatformAdapterRegistry(new[] { new SoftwareWorkspaceAdapter() });
        Assert.Same(registry.Resolve("software.workspace", "file.create.text"), registry.Adapters.Single());
        Assert.Throws<NotSupportedException>(() => registry.Resolve("software.workspace", "shell.execute"));
        Assert.Throws<KeyNotFoundException>(() => registry.Resolve("missing", "file.create.text"));
    }

    [Fact]
    public void RejectsDuplicateAdapterIds()
    {
        Assert.Throws<InvalidOperationException>(() => new PlatformAdapterRegistry(new IPlatformAdapter[]
        {
            new SoftwareWorkspaceAdapter(), new SoftwareWorkspaceAdapter()
        }));
    }
}
