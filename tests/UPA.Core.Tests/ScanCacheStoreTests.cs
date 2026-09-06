using System;
using System.IO;
using Xunit;

namespace UPA.Core.Tests;

public sealed class ScanCacheStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "upa-cache-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveAndLoad_RoundTripsByKey()
    {
        var store = new ScanCacheStore();
        store.Save(_directory, "key-a", "payload");
        Assert.Equal("payload", store.Load(_directory, "key-a"));
        Assert.Null(store.Load(_directory, "key-b"));
        Assert.True(store.Invalidate(_directory, "key-a"));
        Assert.Null(store.Load(_directory, "key-a"));
        Assert.False(store.Invalidate(_directory, "key-a"));
    }

    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
}
