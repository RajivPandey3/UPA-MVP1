using System.Security.Cryptography;
using System.Text;

namespace UPA.Core;

public readonly record struct EntityId(string Value)
{
    public static EntityId FromStableKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(key));

        return new EntityId(
            Convert.ToHexString(bytes)[..32].ToLowerInvariant());
    }

    public static EntityId New() =>
        new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public sealed record Diagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    string? Path = null);

public sealed record ScanContext(
    string ProjectRoot,
    bool ReadOnly = true);

public sealed record ScanResult(
    EntityId ProjectId,
    DateTimeOffset CompletedAt,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    public string? UnityVersion { get; init; }

    public IReadOnlyList<PackageInfo> Packages { get; init; }
        = Array.Empty<PackageInfo>();

    public string? RenderPipelineHint { get; init; }

    public IReadOnlyList<AssemblyDefinitionInfo> Assemblies { get; init; }
        = Array.Empty<AssemblyDefinitionInfo>();

    public string ProjectName { get; init; }
        = string.Empty;

    public string ProjectRoot { get; init; }
        = string.Empty;

    public IReadOnlyList<string> ProjectSettingsFiles { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> BuildTargetHints { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> AssetPaths { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> ProductionScenePaths { get; init; }
        = Array.Empty<string>();

    public int GameObjectCount { get; init; }
}

public sealed record PackageInfo(
    string Name,
    string? Version);

public sealed record AssemblyDefinitionInfo(
    string Path,
    string Name);

public sealed record ProjectScanSnapshot(
    EntityId ProjectId,
    string ProjectName,
    string ProjectRoot,
    string? UnityVersion,
    IReadOnlyList<PackageInfo> Packages,
    IReadOnlyList<string> ProjectSettingsFiles,
    string? RenderPipelineHint,
    IReadOnlyList<string> BuildTargetHints,
    IReadOnlyList<AssemblyDefinitionInfo> Assemblies,
    IReadOnlyList<Diagnostic> Diagnostics);

public interface IProjectScanner
{
    Task<ScanResult> ScanAsync(
        ScanContext context,
        CancellationToken cancellationToken = default);
}