using System.Text.Json;
using UPA.Core;

namespace UPA.Analysis;

public sealed class AssemblyScanner
{
    public AssemblyScanResult Scan(ScanContext context, CancellationToken cancellationToken = default)
    {
        if (!context.ReadOnly)
            throw new InvalidOperationException("MVP-1 AssemblyScanner is read-only.");

        var root = Path.GetFullPath(context.ProjectRoot);
        var assets = Path.Combine(root, "Assets");
        if (!Directory.Exists(assets))
            return new AssemblyScanResult(
                Array.Empty<AssemblyDefinitionModel>(),
                Array.Empty<AssemblyDependencyModel>(),
                Array.Empty<string>(),
                Array.Empty<Diagnostic>());

        var diagnostics = new List<Diagnostic>();

        var files = Directory.EnumerateFiles(
                assets, "*.asmdef", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        var models = files.Select(p => {
            cancellationToken.ThrowIfCancellationRequested();
            return ParseAsmdef(root, p);
        }).ToArray();
        var names = models.ToDictionary(x => x.Name, StringComparer.Ordinal);

        var deps = new List<AssemblyDependencyModel>();
        foreach (var asm in models)
        {
            foreach (var reference in asm.References)
            {
                var resolved = names.ContainsKey(reference);
                if (!resolved)
                    diagnostics.Add(new Diagnostic(
                        "ASMDEF-REF-001",
                        DiagnosticSeverity.Warning,
                        $"Assembly reference '{reference}' could not be resolved inside scanned Assets.",
                        asm.Path));

                deps.Add(new AssemblyDependencyModel(
                    asm.Id, asm.Name, reference, resolved, false));
            }

            foreach (var optional in asm.OptionalUnityReferences)
            {
                deps.Add(new AssemblyDependencyModel(
                    asm.Id, asm.Name, optional, true, true));
            }
        }

        var ownedDirectories = files
            .Select(Path.GetDirectoryName)
            .Where(x => x is not null)
            .Select(x => x!)
            .ToArray();

        var unowned = Directory.EnumerateFiles(
                assets, "*.cs", SearchOption.AllDirectories)
            .Where(cs => !ownedDirectories.Any(dir =>
                IsWithinDirectory(cs, dir)))
            .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/'))
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

    private static AssemblyDefinitionModel ParseAsmdef(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
        var name = Path.GetFileNameWithoutExtension(path);
        var id = EntityId.FromStableKey(relative);
        var d = new List<Diagnostic>();

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
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
                d);
        }
        catch (JsonException ex)
        {
            d.Add(new Diagnostic(
                "ASMDEF-JSON-001",
                DiagnosticSeverity.Error,
                $"Invalid assembly definition JSON: {ex.Message}",
                relative));

            return new AssemblyDefinitionModel(
                id, name, relative,
                Array.Empty<string>(), Array.Empty<string>(),
                Array.Empty<string>(), Array.Empty<string>(),
                Array.Empty<string>(), Array.Empty<string>(),
                true, false, false, false, d);
        }
    }

    private static string? ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString()
            : null;

    private static bool ReadBool(JsonElement root, string name, bool fallback)
        => root.TryGetProperty(name, out var p) &&
           p.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? p.GetBoolean()
            : fallback;

    private static IReadOnlyList<string> ReadArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        return p.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString()!)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsWithinDirectory(string file, string directory)
    {
        var fullFile = Path.GetFullPath(file);
        var fullDir = Path.GetFullPath(directory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return fullFile.StartsWith(fullDir, StringComparison.OrdinalIgnoreCase);
    }
}
