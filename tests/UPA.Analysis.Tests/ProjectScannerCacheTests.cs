using UPA.Analysis;

namespace UPA.Analysis.Tests;

public class ProjectScannerCacheTests
{
    [Fact]
    public void LoadIndex_WhenCacheDoesNotExist_ReturnsFalse()
    {
        var projectRoot =
            Path.Combine(
                Path.GetTempPath(),
                "UPA_Cache_Test_" +
                Guid.NewGuid().ToString("N"));

        try
        {
            using var cache =
                new ProjectScannerCache(projectRoot);

            cache.Clear();

            Assert.False(cache.LoadIndex());
            Assert.Empty(cache.Index);
        }
        finally
        {
            try
            {
                using var cleanup =
                    new ProjectScannerCache(projectRoot);

                cleanup.Clear();
            }
            catch
            {
                // Cleanup must not hide the test result.
            }
        }
    }

    [Fact]
    public void CacheFilePath_IsDeterministicForSameProjectRoot()
    {
        var projectRoot =
            Path.Combine(
                Path.GetTempPath(),
                "UPA_Cache_Test_" +
                Guid.NewGuid().ToString("N"));

        using var first =
            new ProjectScannerCache(projectRoot);

        using var second =
            new ProjectScannerCache(projectRoot);

        Assert.Equal(
            first.CacheFilePath,
            second.CacheFilePath);
    }

    [Fact]
    public void Clear_RemovesExistingCacheFile()
    {
        var projectRoot =
            Path.Combine(
                Path.GetTempPath(),
                "UPA_Cache_Test_" +
                Guid.NewGuid().ToString("N"));

        using var cache =
            new ProjectScannerCache(projectRoot);

        try
        {
            cache.Clear();

            Assert.False(
                File.Exists(cache.CacheFilePath));
        }
        finally
        {
            cache.Clear();
        }
    }

    [Fact]
    public void CommitDelta_ThenLoadIndex_Succeeds()
    {
        var projectRoot =
            Path.Combine(
                Path.GetTempPath(),
                "UPA_Cache_Test_" +
                Guid.NewGuid().ToString("N"));

        using var cache =
            new ProjectScannerCache(projectRoot);

        try
        {
            cache.Clear();

            var model =
                new CSharpScriptModel(
                    UPA.Core.EntityId.FromStableKey(
                        "cache-test-script"),
                    "Assets/Test.cs",
                    "TestNamespace",
                    Array.Empty<CSharpTypeModel>(),
                    Array.Empty<UPA.Core.Diagnostic>());

            var updated =
                new UpdatedModelRecord(
                    "Assets/Test.cs",
                    123,
                    DateTime.UtcNow.Ticks,
                    456UL,
                    model);

            cache.CommitDelta(
                Array.Empty<CacheIndexEntry>(),
                new[] { updated });

            Assert.True(
                File.Exists(cache.CacheFilePath));

            Assert.True(
                cache.LoadIndex());

            Assert.Single(cache.Index);

            Assert.True(
                cache.Index.ContainsKey("Assets/Test.cs"));

            var entry =
                cache.Index["Assets/Test.cs"];

            Assert.Equal(
                123,
                entry.FileLength);

            Assert.Equal(
                456UL,
                entry.ContentHash);

            var loaded =
                cache.LoadModel(entry);

            Assert.NotNull(loaded);
            Assert.Equal(
                "Assets/Test.cs",
                loaded!.Path);
        }
        finally
        {
            cache.Clear();
        }
    }
}