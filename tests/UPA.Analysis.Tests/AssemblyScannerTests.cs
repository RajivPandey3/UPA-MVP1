using UPA.Analysis;
using UPA.Core;

namespace UPA.Analysis.Tests;

public class AssemblyScannerTests
{
    [Fact]
    public void Scan_ParsesAssemblyAndResolvesDependency()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var dir = Path.Combine(root.FullName, "Assets", "Scripts");
            Directory.CreateDirectory(dir);

            File.WriteAllText(
                Path.Combine(dir, "Core.asmdef"),
                "{\"name\":\"Core\",\"autoReferenced\":true}");

            File.WriteAllText(
                Path.Combine(dir, "Gameplay.asmdef"),
                "{\"name\":\"Gameplay\",\"references\":[\"Core\"],\"testAssemblies\":false}");

            var result = new AssemblyScanner().Scan(new ScanContext(root.FullName));

            Assert.Equal(2, result.Assemblies.Count);
            Assert.Contains(result.Dependencies, d =>
                d.SourceAssemblyName == "Gameplay" &&
                d.TargetAssemblyName == "Core" &&
                d.Resolved);
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void Scan_ReportsMissingAssemblyReference()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var dir = Path.Combine(root.FullName, "Assets");
            Directory.CreateDirectory(dir);

            File.WriteAllText(
                Path.Combine(dir, "Gameplay.asmdef"),
                "{\"name\":\"Gameplay\",\"references\":[\"MissingAssembly\"]}");

            var result = new AssemblyScanner().Scan(new ScanContext(root.FullName));

            Assert.Contains(result.Diagnostics, d => d.Code == "ASMDEF-REF-001");
            Assert.Contains(result.Dependencies, d =>
                d.TargetAssemblyName == "MissingAssembly" && !d.Resolved);
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void Scan_EnforcesReadOnly()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                new AssemblyScanner().Scan(new ScanContext(root.FullName, false)));
        }
        finally { root.Delete(true); }
    }
}
