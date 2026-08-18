using UPA.Core;

namespace UPA.Analysis;

public enum CSharpTypeKind { Class, Struct, Interface, Enum, Unknown }

public sealed record SerializedFieldModel(
    string Name, string TypeName, bool IsPrivate, string Path, int Line);

public sealed record CSharpTypeModel(
    EntityId Id,
    string Name,
    CSharpTypeKind Kind,
    string? Namespace,
    string? BaseType,
    IReadOnlyList<string> Attributes,
    IReadOnlyList<string> UnityLifecycleMethods,
    IReadOnlyList<string> RequiredComponents,
    IReadOnlyList<SerializedFieldModel> SerializedFields,
    string Path,
    int Line);

public sealed record CSharpScriptModel(
    EntityId Id,
    string Path,
    string? Namespace,
    IReadOnlyList<CSharpTypeModel> Types,
    IReadOnlyList<Diagnostic> Diagnostics);
