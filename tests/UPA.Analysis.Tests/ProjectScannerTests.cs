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
                scanner.Scan(
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
    public void Scan_HasStableIdentityAndUnityVersion()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            Directory.CreateDirectory(
                Path.Combine(
                    root.FullName,
                    "ProjectSettings"));

            File.WriteAllText(
                Path.Combine(
                    root.FullName,
                    "ProjectSettings",
                    "ProjectVersion.txt"),
                "m_EditorVersion: 6000.0.0f1\n");

            var scanner = new ProjectScanner();

            var a =
                scanner.Scan(
                    new ScanContext(root.FullName));

            var b =
                scanner.Scan(
                    new ScanContext(root.FullName));

            Assert.Equal(a.ProjectId, b.ProjectId);
            Assert.Equal(
                "6000.0.0f1",
                a.UnityVersion);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Scan_ParsesPackagesAndDetectsUrp()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            Directory.CreateDirectory(
                Path.Combine(
                    root.FullName,
                    "Packages"));

            File.WriteAllText(
                Path.Combine(
                    root.FullName,
                    "Packages",
                    "manifest.json"),
                """
                {
                    "dependencies": {
                        "com.unity.render-pipelines.universal": "17.0.0",
                        "com.unity.inputsystem": "1.11.0"
                    }
                }
                """);

            var result =
                new ProjectScanner().Scan(
                    new ScanContext(root.FullName));

            Assert.Equal(
                "URP",
                result.RenderPipelineHint);

            Assert.Equal(
                2,
                result.Packages.Count);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Scan_DiscoversAsmdef()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var dir =
                Path.Combine(
                    root.FullName,
                    "Assets",
                    "Scripts");

            Directory.CreateDirectory(dir);

            File.WriteAllText(
                Path.Combine(
                    dir,
                    "Gameplay.asmdef"),
                "{}");

            var result =
                new ProjectScanner().Scan(
                    new ScanContext(root.FullName));

            Assert.Single(result.Assemblies);

            Assert.Equal(
                "Gameplay",
                result.Assemblies[0].Name);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Scan_DiscoversSceneAndPrefabAssets()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var assets =
                Path.Combine(
                    root.FullName,
                    "Assets");

            Directory.CreateDirectory(assets);

            File.WriteAllText(
                Path.Combine(
                    assets,
                    "Main.unity"),
                "");

            File.WriteAllText(
                Path.Combine(
                    assets,
                    "Player.prefab"),
                "");

            var result =
                new ProjectScanner().Scan(
                    new ScanContext(root.FullName));

            Assert.Contains(
                result.AssetPaths,
                x => x == "Assets/Main.unity");

            Assert.Contains(
                result.AssetPaths,
                x => x == "Assets/Player.prefab");
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Scan_IdentifiesProductionScenes()
    {
        var root = Directory.CreateTempSubdirectory();

        try
        {
            var productionScenes =
                Path.Combine(
                    root.FullName,
                    "Assets",
                    "_Project",
                    "Scenes");

            var otherScenes =
                Path.Combine(
                    root.FullName,
                    "Assets",
                    "Settings",
                    "Scenes");

            Directory.CreateDirectory(
                productionScenes);

            Directory.CreateDirectory(
                otherScenes);

            File.WriteAllText(
                Path.Combine(
                    productionScenes,
                    "Bootstrap.unity"),
                "");

            File.WriteAllText(
                Path.Combine(
                    productionScenes,
                    "Loading.unity"),
                "");

            File.WriteAllText(
                Path.Combine(
                    productionScenes,
                    "MainMenu.unity"),
                "");

            File.WriteAllText(
                Path.Combine(
                    productionScenes,
                    "Main_Gameplay.unity"),
                "");

            File.WriteAllText(
                Path.Combine(
                    otherScenes,
                    "URP2DSceneTemplate.unity"),
                "");

            var result =
                new ProjectScanner().Scan(
                    new ScanContext(root.FullName));

            Assert.Equal(
                4,
                result.ProductionScenePaths.Count);

            Assert.Contains(
                "Assets/_Project/Scenes/Bootstrap.unity",
                result.ProductionScenePaths);

            Assert.Contains(
                "Assets/_Project/Scenes/Loading.unity",
                result.ProductionScenePaths);

            Assert.Contains(
                "Assets/_Project/Scenes/MainMenu.unity",
                result.ProductionScenePaths);

            Assert.Contains(
                "Assets/_Project/Scenes/Main_Gameplay.unity",
                result.ProductionScenePaths);

            Assert.DoesNotContain(
                "Assets/Settings/Scenes/URP2DSceneTemplate.unity",
                result.ProductionScenePaths);
        }
        finally
        {
            root.Delete(true);
        }
    }
[Fact]
public void Scan_CountsGameObjectsInProductionScenes()
{
    var root = Directory.CreateTempSubdirectory();

    try
    {
        var scenes =
            Path.Combine(
                root.FullName,
                "Assets",
                "_Project",
                "Scenes");

        Directory.CreateDirectory(scenes);

        File.WriteAllText(
            Path.Combine(scenes, "Bootstrap.unity"),
            """
            --- !u!1 &100
            GameObject:
              m_Name: Bootstrap
            --- !u!1 &101
            GameObject:
              m_Name: Systems
            """);

        File.WriteAllText(
            Path.Combine(scenes, "Loading.unity"),
            """
            --- !u!1 &200
            GameObject:
              m_Name: Loading
            """);

        File.WriteAllText(
            Path.Combine(scenes, "MainMenu.unity"),
            """
            --- !u!1 &300
            GameObject:
              m_Name: MainMenu
            --- !u!1 &301
            GameObject:
              m_Name: Canvas
            --- !u!1 &302
            GameObject:
              m_Name: Camera
            """);

        File.WriteAllText(
            Path.Combine(scenes, "Main_Gameplay.unity"),
            """
            --- !u!1 &400
            GameObject:
              m_Name: Gameplay
            """);

        var result =
            new ProjectScanner().Scan(
                new ScanContext(root.FullName));

        Assert.Equal(
            7,
            result.GameObjectCount);
    }
    finally
    {
        root.Delete(true);
    }
}
}