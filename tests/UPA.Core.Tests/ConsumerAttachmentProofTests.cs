using System;
using System.IO;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ConsumerAttachmentProofTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("upa-consumer-").FullName;

    [Fact]
    public void FreshConsumerAttachmentCanBeLoadedVerifiedAndSafelyRemoved()
    {
        var attachment = Path.Combine(root, ".upa", "attachment.json");
        var owned = Path.Combine(root, ".upa", "adapter.marker");
        Directory.CreateDirectory(Path.GetDirectoryName(owned)!);
        File.WriteAllText(owned, "upa-owned-v1");
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(owned)));
        var manifest = new ProjectAttachmentManifest("1", EntityId.FromStableKey("consumer"), "filesystem", "1.0", new[] { "read.project" }, new[] { new AttachmentFile(".upa/adapter.marker", hash) }, DateTimeOffset.UtcNow);
        var store = new AttachmentManifestStore();
        store.Save(attachment, manifest);
        var loaded = store.Load(attachment);
        var removable = new AttachmentOwnershipGuard().GetRemovableFiles(root, loaded);
        Assert.Contains(owned, removable);
        Assert.True(File.Exists(attachment));
    }

    public void Dispose() => Directory.Delete(root, true);
}
