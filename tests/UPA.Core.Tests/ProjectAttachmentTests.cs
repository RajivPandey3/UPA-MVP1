using System;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ProjectAttachmentTests
{
    [Fact]
    public void ManifestTracksAdapterPermissionsAndOwnedHashes()
    {
        var manifest = new ProjectAttachmentManifest("1", EntityId.FromStableKey("project"), "unity", "1.0",
            new[] { "read.project" }, new[] { new AttachmentFile("Assets/UPA.meta", "ABC") }, DateTimeOffset.UtcNow);
        Assert.Equal("unity", manifest.AdapterId);
        Assert.Equal("ABC", Assert.Single(manifest.OwnedFiles).Sha256);
    }

    [Fact]
    public void RejectsMissingAdapterIdentity()
    {
        Assert.Throws<ArgumentException>(() => new ProjectAttachmentManifest("1", EntityId.New(), "", "1", Array.Empty<string>(), Array.Empty<AttachmentFile>(), DateTimeOffset.UtcNow));
    }
}
