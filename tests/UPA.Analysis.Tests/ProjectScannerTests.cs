using UPA.Analysis;
using UPA.Core;

namespace UPA.Analysis.Tests;

public class ProjectScannerTests
{
    [Fact]
    public void ReadOnlyGuard_RejectsWriteMode()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var scanner = new ProjectScanner();
            Assert.Throws<InvalidOperationException>(() =>
                scanner.Scan(new ScanContext(root.FullName, false)));
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void Scan_HasStableIdentityAndUnityVersion()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, "ProjectSettings"));
            File.WriteAllText(
                Path.Combine(root.FullName, "ProjectSettings", "ProjectVersion.txt"),
                "m_EditorVersion: 6000.0.0f1\n");

            var scanner = new ProjectScanner();
            var a = scanner.Scan(new ScanContext(root.FullName));
            var b = scanner.Scan(new ScanContext(root.FullName));

            Assert.Equal(a.ProjectId, b.ProjectId);
            Assert.Equal("6000.0.0f1", a.UnityVersion);
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void Scan_ParsesPackagesAndDetectsUrp()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, "Packages"));
            File.WriteAllText(
                Path.Combine(root.FullName, "Packages", "manifest.json"),
                """{"dependencies":{"com.unity.render-pipelines.universal":"17.0.0","com.unity.inputsystem":"1.11.0"}}""");

            var result = new ProjectScanner().Scan(new ScanContext(root.FullName));

            Assert.Equal("URP", result.RenderPipelineHint);
            Assert.Equal(2, result.Packages.Count);
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void Scan_DiscoversAsmdef()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var dir = Path.Combine(root.FullName, "Assets", "Scripts");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "Gameplay.asmdef"), "{}");

            var result = new ProjectScanner().Scan(new ScanContext(root.FullName));

            Assert.Single(result.Assemblies);
            Assert.Equal("Gameplay", result.Assemblies[0].Name);
        }
        finally { root.Delete(true); }
    }
}
