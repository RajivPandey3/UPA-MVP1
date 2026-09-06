using System;
using System.IO;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class AttachmentOwnershipGuardTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("upa-owned-").FullName;

    [Fact]
    public void RemovesOnlyManifestOwnedUnchangedFiles()
    {
        var path = Path.Combine(root, "owned.txt");
        File.WriteAllText(path, "owned");
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));
        var manifest = new ProjectAttachmentManifest("1", EntityId.New(), "adapter", "1", Array.Empty<string>(), new[] { new AttachmentFile("owned.txt", hash) }, DateTimeOffset.UtcNow);
        Assert.Equal(path, Assert.Single(new AttachmentOwnershipGuard().GetRemovableFiles(root, manifest)));
    }

    [Fact]
    public void DoesNotClaimModifiedFile()
    {
        var path = Path.Combine(root, "owned.txt");
        File.WriteAllText(path, "changed");
        var manifest = new ProjectAttachmentManifest("1", EntityId.New(), "adapter", "1", Array.Empty<string>(), new[] { new AttachmentFile("owned.txt", "wrong") }, DateTimeOffset.UtcNow);
        Assert.Empty(new AttachmentOwnershipGuard().GetRemovableFiles(root, manifest));
    }

    public void Dispose() => Directory.Delete(root, true);
}
