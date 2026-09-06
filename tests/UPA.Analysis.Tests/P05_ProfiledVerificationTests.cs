using UPA.Analysis;
using UPA.Core;

namespace UPA.Analysis.Tests;

public sealed class P05_ProfiledVerificationTests
{
    [Fact]
    public async Task ScanAsyncMeetsProfileAndReturnsResult()
    {
        var root = CreateProject(PerformanceProfile.Files(10000));
        try
        {
            var result = await new ProjectScanner().ScanAsync(new ScanContext(root));
            Assert.NotNull(result);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task CancellationIsHonoredForProfile()
    {
        var root = CreateProject(PerformanceProfile.Files(10000));
        try
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new ProjectScanner().ScanAsync(new ScanContext(root), cancellation.Token));
        }
        finally { Directory.Delete(root, true); }
    }

    private static string CreateProject(int count)
    {
        var root = Directory.CreateTempSubdirectory("upa-profile-").FullName;
        var assets = Directory.CreateDirectory(Path.Combine(root, "Assets"));
        Directory.CreateDirectory(Path.Combine(root, "ProjectSettings"));
        File.WriteAllText(Path.Combine(root, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.0.0f1");
        for (var index = 0; index < count; index++)
            File.WriteAllText(Path.Combine(assets.FullName, $"Dummy_{index}.cs"), "public class Dummy {}");
        return root;
    }
}
