using System;
using System.IO;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class AttachmentManifestStoreTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("upa-manifest-").FullName;

    [Fact]
    public void SavesAndLoadsManifest()
    {
        var path = Path.Combine(root, ".upa", "attachment.json");
        var manifest = new ProjectAttachmentManifest("1", EntityId.FromStableKey("p"), "filesystem", "1.0", new[] { "read" }, Array.Empty<AttachmentFile>(), DateTimeOffset.UtcNow);
        var store = new AttachmentManifestStore();
        store.Save(path, manifest);
        Assert.Equal(manifest.AdapterId, store.Load(path).AdapterId);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void RejectsCorruptManifest()
    {
        var path = Path.Combine(root, "bad.json");
        File.WriteAllText(path, "not-json");
        Assert.Throws<InvalidDataException>(() => new AttachmentManifestStore().Load(path));
    }

    public void Dispose() => Directory.Delete(root, true);
}
