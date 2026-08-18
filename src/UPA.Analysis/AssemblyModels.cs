using UPA.Core;

namespace UPA.Analysis;

public sealed record AssemblyDefinitionModel(
    EntityId Id,
    string Name,
    string Path,
    IReadOnlyList<string> References,
    IReadOnlyList<string> OptionalUnityReferences,
    IReadOnlyList<string> IncludePlatforms,
    IReadOnlyList<string> ExcludePlatforms,
    IReadOnlyList<string> DefineConstraints,
    IReadOnlyList<string> VersionDefines,
    bool AutoReferenced,
    bool NoEngineReferences,
    bool OverrideReferences,
    bool TestAssembly,
    IReadOnlyList<Diagnostic> Diagnostics);

public sealed record AssemblyDependencyModel(
    EntityId SourceAssemblyId,
    string SourceAssemblyName,
    string TargetAssemblyName,
    bool Resolved,
    bool Optional);

public sealed record AssemblyScanResult(
    IReadOnlyList<AssemblyDefinitionModel> Assemblies,
    IReadOnlyList<AssemblyDependencyModel> Dependencies,
    IReadOnlyList<string> UnownedScripts,
    IReadOnlyList<Diagnostic> Diagnostics);
