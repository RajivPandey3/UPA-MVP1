using System.Text.Json;
using UPA.Core;

namespace UPA.Analysis;

public sealed class AssemblyScanner
{
    public AssemblyScanResult Scan(
        ScanContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.ReadOnly)
            throw new InvalidOperationException(
                "MVP-1 AssemblyScanner is read-only.");

        var root = Path.GetFullPath(context.ProjectRoot);
        var assets = Path.Combine(root, "Assets");

        if (!Directory.Exists(assets))
        {
            return new AssemblyScanResult(
                Array.Empty<AssemblyDefinitionModel>(),
                Array.Empty<AssemblyDependencyModel>(),
                Array.Empty<string>(),
                Array.Empty<Diagnostic>());
        }

        var diagnostics = new List<Diagnostic>();

        var files = Directory.EnumerateFiles(
                assets,
                "*.asmdef",
                SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        var models = files
            .Select(p =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ParseAsmdef(root, p);
            })
            .ToArray();

        var names = models.ToDictionary(
            x => x.Name,
            StringComparer.Ordinal);

        // Discover package-owned assembly definitions by their .meta GUID.
        // This allows GUID:xxxx references in project asmdefs to resolve
        // against Unity package assemblies found in Library/PackageCache.
        var packageAssemblies =
            DiscoverPackageAssemblies(root);

        var deps = new List<AssemblyDependencyModel>();

        foreach (var asm in models)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var reference in asm.References)
            {
                var isProjectAssembly =
                    names.ContainsKey(reference);

                var isKnownUnityPackageAssembly =
                    reference == "Unity.InputSystem";

                var isPackageGuidReference =
                    packageAssemblies.ContainsKey(reference);

                var resolved =
                    isProjectAssembly ||
                    isKnownUnityPackageAssembly ||
                    isPackageGuidReference;

                var optional =
                    isKnownUnityPackageAssembly ||
                    isPackageGuidReference;

                var targetAssemblyName =
                    isPackageGuidReference
                        ? packageAssemblies[reference]
                        : reference;

                if (!resolved)
                {
                    diagnostics.Add(new Diagnostic(
                        "ASMDEF-REF-001",
                        DiagnosticSeverity.Warning,
                        $"Assembly reference '{reference}' could not be resolved inside scanned Assets.",
                        asm.Path));
                }

                deps.Add(new AssemblyDependencyModel(
                    asm.Id,
                    asm.Name,
                    targetAssemblyName,
                    resolved,
                    optional));
            }

            foreach (var optional in asm.OptionalUnityReferences)
            {
                deps.Add(new AssemblyDependencyModel(
                    asm.Id,
                    asm.Name,
                    optional,
                    true,
                    true));
            }
        }

        var ownedDirectories = files
            .Select(Path.GetDirectoryName)
            .Where(x => x is not null)
            .Select(x => x!)
            .ToArray();

        var unowned = Directory.EnumerateFiles(
                assets,
                "*.cs",
                SearchOption.AllDirectories)
            .Where(cs => !ownedDirectories.Any(dir =>
                IsWithinDirectory(cs, dir)))
            .Select(p =>
                Path.GetRelativePath(root, p)
                    .Replace('\\', '/'))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        return new AssemblyScanResult(
            models,
            deps,
            unowned,
            diagnostics
                .Concat(models.SelectMany(x => x.Diagnostics))
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Path)
                .ToArray());
    }

    private static AssemblyDefinitionModel ParseAsmdef(
        string root,
        string path)
    {
        var relative =
            Path.GetRelativePath(root, path)
                .Replace('\\', '/');

        var name =
            Path.GetFileNameWithoutExtension(path);

        var id =
            EntityId.FromStableKey(relative);

        var diagnostics =
            new List<Diagnostic>();

        try
        {
            using var doc =
                JsonDocument.Parse(
                    File.ReadAllText(path));

            var r = doc.RootElement;

            return new AssemblyDefinitionModel(
                id,
                ReadString(r, "name") ?? name,
                relative,
                ReadArray(r, "references"),
                ReadArray(r, "optionalUnityReferences"),
                ReadArray(r, "includePlatforms"),
                ReadArray(r, "excludePlatforms"),
                ReadArray(r, "defineConstraints"),
                ReadArray(r, "versionDefines"),
                ReadBool(r, "autoReferenced", true),
                ReadBool(r, "noEngineReferences", false),
                ReadBool(r, "overrideReferences", false),
                ReadBool(r, "testAssemblies", false),
                diagnostics);
        }
        catch (JsonException ex)
        {
            diagnostics.Add(new Diagnostic(
                "ASMDEF-JSON-001",
                DiagnosticSeverity.Error,
                $"Invalid assembly definition JSON: {ex.Message}",
                relative));

            return new AssemblyDefinitionModel(
                id,
                name,
                relative,
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                true,
                false,
                false,
                false,
                diagnostics);
        }
    }

    private static IReadOnlyDictionary<string, string>
        DiscoverPackageAssemblies(string root)
    {
        var result =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

        var packageCache =
            Path.Combine(
                root,
                "Library",
                "PackageCache");

        if (!Directory.Exists(packageCache))
            return result;

        IEnumerable<string> asmdefFiles;

        try
        {
            asmdefFiles = Directory.EnumerateFiles(
                packageCache,
                "*.asmdef",
                SearchOption.AllDirectories);
        }
        catch (IOException)
        {
            return result;
        }
        catch (UnauthorizedAccessException)
        {
            return result;
        }

        foreach (var asmdef in asmdefFiles
                     .OrderBy(x => x, StringComparer.Ordinal))
        {
            var meta = asmdef + ".meta";

            if (!File.Exists(meta))
                continue;

            var guid = ReadMetaGuid(meta);

            if (string.IsNullOrWhiteSpace(guid))
                continue;

            string? name = null;

            try
            {
                using var doc =
                    JsonDocument.Parse(
                        File.ReadAllText(asmdef));

                name = ReadString(
                    doc.RootElement,
                    "name");
            }
            catch (JsonException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(name))
                continue;

            result[$"GUID:{guid}"] = name;
        }

        return result;
    }

    private static string? ReadMetaGuid(string path)
    {
        try
        {
            foreach (var line in File.ReadLines(path))
            {
                var trimmed = line.Trim();

                if (!trimmed.StartsWith(
                        "guid:",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return trimmed["guid:".Length..].Trim();
            }
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }

        return null;
    }

    private static string? ReadString(
        JsonElement root,
        string name)
        => root.TryGetProperty(
                name,
                out var property) &&
            property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool ReadBool(
        JsonElement root,
        string name,
        bool fallback)
        => root.TryGetProperty(
                name,
                out var property) &&
            property.ValueKind is
                JsonValueKind.True or
                JsonValueKind.False
            ? property.GetBoolean()
            : fallback;

    private static IReadOnlyList<string> ReadArray(
        JsonElement root,
        string name)
    {
        if (!root.TryGetProperty(
                name,
                out var property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        return property
            .EnumerateArray()
            .Where(x =>
                x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString()!)
            .OrderBy(
                x => x,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsWithinDirectory(
        string file,
        string directory)
    {
        var fullFile =
            Path.GetFullPath(file);

        var fullDir =
            Path.GetFullPath(directory)
                .TrimEnd(
                    Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        return fullFile.StartsWith(
            fullDir,
            StringComparison.OrdinalIgnoreCase);
    }
}