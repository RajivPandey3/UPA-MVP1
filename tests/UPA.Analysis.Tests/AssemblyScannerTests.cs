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
            var dir = Path.Combine(
                root.FullName,
                "Assets",
                "Scripts");

            Directory.CreateDirectory(dir);

            File.WriteAllText(
                Path.Combine(dir, "Core.asmdef"),
                """
                {
                    "name":"Core",
                    "autoReferenced":true
                }
                """);

            File.WriteAllText(
                Path.Combine(dir, "Gameplay.asmdef"),
                """
                {
                    "name":"Gameplay",
                    "references":["Core"],
                    "testAssemblies":false
                }
                """);

            var result =
                new AssemblyScanner().Scan(
                    new ScanContext(root.FullName));

            Assert.Equal(
                2,
                result.Assemblies.Count);

            Assert.Contains(
                result.Dependencies,
                d =>
                    d.SourceAssemblyName == "Gameplay" &&
                    d.TargetAssemblyName == "Core" &&
                    d.Resolved);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Scan_ReportsMissingAssemblyReference()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var dir = Path.Combine(
                root.FullName,
                "Assets");

            Directory.CreateDirectory(dir);

            File.WriteAllText(
                Path.Combine(dir, "Gameplay.asmdef"),
                """
                {
                    "name":"Gameplay",
                    "references":["MissingAssembly"]
                }
                """);

            var result =
                new AssemblyScanner().Scan(
                    new ScanContext(root.FullName));

            Assert.Contains(
                result.Diagnostics,
                d => d.Code == "ASMDEF-REF-001");

            Assert.Contains(
                result.Dependencies,
                d =>
                    d.TargetAssemblyName ==
                        "MissingAssembly" &&
                    !d.Resolved);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Scan_EnforcesReadOnly()
    {
        var root =
            Directory.CreateTempSubdirectory();

        try
        {
            Assert.Throws<InvalidOperationException>(
                () =>
                    new AssemblyScanner().Scan(
                        new ScanContext(
                            root.FullName,
                            false)));
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Scan_RecognizesKnownUnityPackageAssemblyReference()
    {
        var root =
            Directory.CreateTempSubdirectory();

        try
        {
            var dir = Path.Combine(
                root.FullName,
                "Assets");

            Directory.CreateDirectory(dir);

            File.WriteAllText(
                Path.Combine(
                    dir,
                    "Foundation.asmdef"),
                """
                {
                    "name":"Foundation",
                    "references":["Unity.InputSystem"]
                }
                """);

            var result =
                new AssemblyScanner().Scan(
                    new ScanContext(root.FullName));

            Assert.Contains(
                result.Dependencies,
                d =>
                    d.SourceAssemblyName ==
                        "Foundation" &&
                    d.TargetAssemblyName ==
                        "Unity.InputSystem" &&
                    d.Resolved &&
                    d.Optional);

            Assert.DoesNotContain(
                result.Diagnostics,
                d => d.Code == "ASMDEF-REF-001");
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Scan_ResolvesUnityPackageAssemblyByGuid()
    {
        var root =
            Directory.CreateTempSubdirectory();

        try
        {
            var assets =
                Path.Combine(
                    root.FullName,
                    "Assets");

            Directory.CreateDirectory(assets);

            var package =
                Path.Combine(
                    root.FullName,
                    "Library",
                    "PackageCache",
                    "com.unity.ugui@24b10291b18f",
                    "Runtime",
                    "TMP");

            Directory.CreateDirectory(package);

            File.WriteAllText(
                Path.Combine(
                    assets,
                    "Gameplay.asmdef"),
                """
                {
                    "name":"Gameplay",
                    "references":[
                        "GUID:6055be8ebefd69e48b49212b09b47b2f"
                    ]
                }
                """);

            File.WriteAllText(
                Path.Combine(
                    package,
                    "Unity.TextMeshPro.asmdef"),
                """
                {
                    "name":"Unity.TextMeshPro"
                }
                """);

            File.WriteAllText(
                Path.Combine(
                    package,
                    "Unity.TextMeshPro.asmdef.meta"),
                """
                fileFormatVersion: 2
                guid: 6055be8ebefd69e48b49212b09b47b2f
                """);

            var result =
                new AssemblyScanner().Scan(
                    new ScanContext(root.FullName));

            Assert.Contains(
                result.Dependencies,
                d =>
                    d.SourceAssemblyName ==
                        "Gameplay" &&
                    d.TargetAssemblyName ==
                        "Unity.TextMeshPro" &&
                    d.Resolved &&
                    d.Optional);

            Assert.DoesNotContain(
                result.Diagnostics,
                d => d.Code == "ASMDEF-REF-001");
        }
        finally
        {
            root.Delete(true);
        }
    }
}