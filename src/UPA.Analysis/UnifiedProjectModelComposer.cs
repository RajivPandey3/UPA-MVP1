using UPA.Core;
using UPA.ProjectModel;

namespace UPA.Analysis;

public sealed class UnifiedProjectModelComposer
{
    public UnifiedProjectModel Compose(
        ScanResult scan,
        IReadOnlyList<CSharpScriptModel> scripts,
        AssemblyScanResult assemblies)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentNullException.ThrowIfNull(scripts);
        ArgumentNullException.ThrowIfNull(assemblies);

        var sceneCount = scan.AssetPaths.Count(
            x => x.EndsWith(
                ".unity",
                StringComparison.OrdinalIgnoreCase));

        var prefabCount = scan.AssetPaths.Count(
            x => x.EndsWith(
                ".prefab",
                StringComparison.OrdinalIgnoreCase));

        var assetCount = scan.AssetPaths.Count;

        var counts = new ProjectCounts(
            Scripts: scripts.Count,
            Types: scripts.Sum(x => x.Types.Count),
            Assemblies: assemblies.Assemblies.Count,
            Scenes: scan.ProductionScenePaths.Count,
            GameObjects: scan.GameObjectCount,
            Prefabs: prefabCount,
            Assets: assetCount,
            References: assemblies.Dependencies.Count,
            UnresolvedReferences: assemblies.Dependencies.Count(
                x => !x.Resolved && !x.Optional),
            Diagnostics:
                scan.Diagnostics.Count +
                scripts.Sum(x => x.Diagnostics.Count) +
                assemblies.Diagnostics.Count);

        var builder = new ProjectModelBuilder();

        if (!string.IsNullOrWhiteSpace(scan.UnityVersion))
        {
            builder.AddFact(
                "unity.version",
                scan.UnityVersion,
                "ProjectScanner");
        }

        if (!string.IsNullOrWhiteSpace(scan.RenderPipelineHint))
        {
            builder.AddFact(
                "render.pipeline",
                scan.RenderPipelineHint,
                "ProjectScanner");
        }

        builder.AddFact(
            "scanner.csharp.scripts",
            scripts.Count.ToString(),
            "CSharpScanner");

        builder.AddFact(
            "scanner.csharp.types",
            counts.Types.ToString(),
            "CSharpScanner");

        builder.AddFact(
            "scanner.assemblies",
            assemblies.Assemblies.Count.ToString(),
            "AssemblyScanner");

        builder.AddFact(
            "scanner.references",
            assemblies.Dependencies.Count.ToString(),
            "AssemblyScanner");

        builder.AddFact(
            "scanner.scenes",
            sceneCount.ToString(),
            "ProjectScanner");

        builder.AddFact(
            "scanner.prefabs",
            prefabCount.ToString(),
            "ProjectScanner");

        builder.AddFact(
            "scanner.assets",
            assetCount.ToString(),
            "ProjectScanner");

        return builder.Build(
            scan.ProjectId.ToString(),
            scan.ProjectName,
            scan.ProjectRoot,
            scan.UnityVersion,
            scan.RenderPipelineHint,
            counts);
    }
}