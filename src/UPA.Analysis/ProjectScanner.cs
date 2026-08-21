using System.Text.Json;
using UPA.Core;

namespace UPA.Analysis;

public sealed class ProjectScanner : IProjectScanner
{
    public ScanResult Scan(ScanContext context)
        => ScanCore(context);

    public Task<ScanResult> ScanAsync(
        ScanContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(
            () => ScanCore(context, cancellationToken),
            cancellationToken);
    }

    private static ScanResult ScanCore(
        ScanContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.ReadOnly)
            throw new InvalidOperationException(
                "MVP-1 scanners are read-only.");

        if (string.IsNullOrWhiteSpace(context.ProjectRoot))
            throw new ArgumentException(
                "Project root is required.",
                nameof(context));

        var root = Path.GetFullPath(context.ProjectRoot);

        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException(root);

        var diagnostics = new List<Diagnostic>();

        var projectId =
            EntityId.FromStableKey(root);

        var projectName =
            new DirectoryInfo(root).Name;

        cancellationToken.ThrowIfCancellationRequested();

        var unityVersion =
            ReadUnityVersion(root, diagnostics);

        var packages =
            ReadPackages(root, diagnostics);

        var pipeline =
            DetectRenderPipeline(packages);

        var assemblies =
            DiscoverAssemblies(
                root,
                diagnostics,
                cancellationToken);

        var settingsFiles =
            DiscoverProjectSettings(root);

        var assetPaths =
            DiscoverAssets(
                root,
                cancellationToken);

        var productionScenePaths =
            DiscoverProductionScenes(
                root,
                cancellationToken);

        var gameObjectCount =
            CountGameObjects(
                root,
                productionScenePaths,
                cancellationToken);

        return new ScanResult(
            projectId,
            DateTimeOffset.UtcNow,
            diagnostics
                .OrderBy(
                    x => x.Code,
                    StringComparer.Ordinal)
                .ThenBy(
                    x => x.Path,
                    StringComparer.Ordinal)
                .ThenBy(
                    x => x.Message,
                    StringComparer.Ordinal)
                .ToArray())
        {
            ProjectName = projectName,
            ProjectRoot = root,
            UnityVersion = unityVersion,
            Packages = packages,
            RenderPipelineHint = pipeline,
            Assemblies = assemblies,
            ProjectSettingsFiles = settingsFiles,
            AssetPaths = assetPaths,
            ProductionScenePaths = productionScenePaths,
            GameObjectCount = gameObjectCount
        };
    }

    private static string? ReadUnityVersion(
        string root,
        List<Diagnostic> diagnostics)
    {
        var path = Path.Combine(
            root,
            "ProjectSettings",
            "ProjectVersion.txt");

        if (!File.Exists(path))
            return null;

        foreach (var line in File.ReadLines(path))
        {
            const string prefix = "m_EditorVersion:";

            if (!line.StartsWith(
                    prefix,
                    StringComparison.Ordinal))
                continue;

            var value =
                line[prefix.Length..].Trim();

            return string.IsNullOrWhiteSpace(value)
                ? null
                : value;
        }

        diagnostics.Add(
            new Diagnostic(
                "PROJECT-UNITY-001",
                DiagnosticSeverity.Warning,
                "ProjectVersion.txt does not contain m_EditorVersion.",
                Path.GetRelativePath(root, path)
                    .Replace('\\', '/')));

        return null;
    }

    private static IReadOnlyList<PackageInfo> ReadPackages(
        string root,
        List<Diagnostic> diagnostics)
    {
        var path = Path.Combine(
            root,
            "Packages",
            "manifest.json");

        if (!File.Exists(path))
            return Array.Empty<PackageInfo>();

        try
        {
            using var doc =
                JsonDocument.Parse(
                    File.ReadAllText(path));

            if (!doc.RootElement.TryGetProperty(
                    "dependencies",
                    out var deps) ||
                deps.ValueKind != JsonValueKind.Object)
            {
                return Array.Empty<PackageInfo>();
            }

            return deps
                .EnumerateObject()
                .Select(
                    p => new PackageInfo(
                        p.Name,
                        p.Value.ValueKind ==
                            JsonValueKind.String
                            ? p.Value.GetString()
                            : null))
                .OrderBy(
                    x => x.Name,
                    StringComparer.Ordinal)
                .ToArray();
        }
        catch (JsonException ex)
        {
            diagnostics.Add(
                new Diagnostic(
                    "PROJECT-PACKAGE-001",
                    DiagnosticSeverity.Error,
                    $"Invalid Packages/manifest.json: {ex.Message}",
                    "Packages/manifest.json"));

            return Array.Empty<PackageInfo>();
        }
    }

    private static string? DetectRenderPipeline(
        IReadOnlyList<PackageInfo> packages)
    {
        if (packages.Any(
                x => x.Name.Equals(
                    "com.unity.render-pipelines.universal",
                    StringComparison.OrdinalIgnoreCase)))
        {
            return "URP";
        }

        if (packages.Any(
                x => x.Name.Equals(
                    "com.unity.render-pipelines.high-definition",
                    StringComparison.OrdinalIgnoreCase)))
        {
            return "HDRP";
        }

        if (packages.Any(
                x => x.Name.Equals(
                    "com.unity.render-pipelines.core",
                    StringComparison.OrdinalIgnoreCase)))
        {
            return "SRP";
        }

        return null;
    }

    private static IReadOnlyList<AssemblyDefinitionInfo>
        DiscoverAssemblies(
            string root,
            List<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
    {
        var assets =
            Path.Combine(root, "Assets");

        if (!Directory.Exists(assets))
            return Array.Empty<AssemblyDefinitionInfo>();

        return Directory.EnumerateFiles(
                assets,
                "*.asmdef",
                SearchOption.AllDirectories)
            .Select(
                path =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var relative =
                        Path.GetRelativePath(
                            root,
                            path)
                            .Replace('\\', '/');

                    var name =
                        Path.GetFileNameWithoutExtension(path);

                    try
                    {
                        using var doc =
                            JsonDocument.Parse(
                                File.ReadAllText(path));

                        if (doc.RootElement.TryGetProperty(
                                "name",
                                out var n) &&
                            n.ValueKind ==
                                JsonValueKind.String &&
                            !string.IsNullOrWhiteSpace(
                                n.GetString()))
                        {
                            name = n.GetString()!;
                        }
                    }
                    catch (JsonException ex)
                    {
                        diagnostics.Add(
                            new Diagnostic(
                                "PROJECT-ASMDEF-001",
                                DiagnosticSeverity.Error,
                                $"Invalid assembly definition JSON: {ex.Message}",
                                relative));
                    }

                    return new AssemblyDefinitionInfo(
                        relative,
                        name);
                })
            .OrderBy(
                x => x.Path,
                StringComparer.Ordinal)
            .ThenBy(
                x => x.Name,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string>
        DiscoverProjectSettings(string root)
    {
        var settings =
            Path.Combine(
                root,
                "ProjectSettings");

        if (!Directory.Exists(settings))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(
                settings,
                "*",
                SearchOption.TopDirectoryOnly)
            .Select(
                x => Path.GetRelativePath(
                        root,
                        x)
                    .Replace('\\', '/'))
            .OrderBy(
                x => x,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string>
        DiscoverAssets(
            string root,
            CancellationToken cancellationToken)
    {
        var assets =
            Path.Combine(
                root,
                "Assets");

        if (!Directory.Exists(assets))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(
                assets,
                "*",
                SearchOption.AllDirectories)
            .Where(
                path =>
                    path.EndsWith(
                        ".unity",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(
                        ".prefab",
                        StringComparison.OrdinalIgnoreCase))
            .Select(
                path =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    return Path.GetRelativePath(
                            root,
                            path)
                        .Replace('\\', '/');
                })
            .OrderBy(
                x => x,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string>
        DiscoverProductionScenes(
            string root,
            CancellationToken cancellationToken)
    {
        var productionScenes =
            Path.Combine(
                root,
                "Assets",
                "_Project",
                "Scenes");

        if (!Directory.Exists(productionScenes))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(
                productionScenes,
                "*.unity",
                SearchOption.AllDirectories)
            .Select(
                path =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    return Path.GetRelativePath(
                            root,
                            path)
                        .Replace('\\', '/');
                })
            .OrderBy(
                x => x,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static int CountGameObjects(
        string root,
        IReadOnlyList<string> productionScenePaths,
        CancellationToken cancellationToken)
    {
        var count = 0;

        foreach (var relativePath in productionScenePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path =
                Path.Combine(
                    root,
                    relativePath.Replace(
                        '/',
                        Path.DirectorySeparatorChar));

            if (!File.Exists(path))
                continue;

            foreach (var line in File.ReadLines(path))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (line.StartsWith(
                        "--- !u!1 &",
                        StringComparison.Ordinal))
                {
                    count++;
                }
            }
        }

        return count;
    }
}