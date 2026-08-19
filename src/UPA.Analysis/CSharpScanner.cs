using System.Text.RegularExpressions;
using UPA.Core;

namespace UPA.Analysis;

public sealed class CSharpScanner
{
    private static readonly Regex NamespaceRegex =
        new(@"\bnamespace\s+([A-Za-z_][\w.]*)", RegexOptions.Compiled);

    private static readonly Regex TypeRegex =
        new(@"(?<attrs>(?:\s*\[[^\]]+\]\s*)*)(?:public|private|protected|internal|abstract|sealed|partial|static|readonly|new|unsafe|\s)*\b(?<kind>class|struct|interface|enum)\s+(?<name>[A-Za-z_][\w]*)(?:\s*:\s*(?<base>[^\{\n]+))?",
            RegexOptions.Compiled);

    private static readonly Regex RequireRegex =
        new(@"RequireComponent\s*\(\s*typeof\s*\(\s*(?<name>[A-Za-z_][\w.]*)\s*\)\s*\)", RegexOptions.Compiled);

    private static readonly Regex SerializedRegex =
        new(@"(?<attrs>(?:\[[^\]]+\]\s*)*)(?<access>private|public|protected)?\s*(?<type>[A-Za-z_][\w.<>,\[\]?]*)\s+(?<name>[A-Za-z_][\w]*)\s*(?:=\s*[^;]+)?;",
            RegexOptions.Compiled);

    private static readonly Regex AttrRegex = new(@"\[([^\]]+)\]", RegexOptions.Compiled);

    private static readonly Regex LifecycleRegex =
        new(@"\b(Awake|OnEnable|Start|Update|FixedUpdate|LateUpdate|OnDisable|OnDestroy|OnTriggerEnter|OnCollisionEnter)\s*\(", RegexOptions.Compiled);

    private static readonly string[] LifecycleMethods =
        ["Awake", "OnEnable", "Start", "Update", "FixedUpdate", "LateUpdate",
         "OnDisable", "OnDestroy", "OnTriggerEnter", "OnCollisionEnter"];

    public IReadOnlyList<CSharpScriptModel> Scan(ScanContext context, CancellationToken cancellationToken = default)
    {
        if (!context.ReadOnly)
            throw new InvalidOperationException("MVP-1 CSharpScanner is read-only.");

        var root = Path.GetFullPath(context.ProjectRoot);
        var assets = Path.Combine(root, "Assets");
        if (!Directory.Exists(assets))
            return Array.Empty<CSharpScriptModel>();

        var files = Directory.EnumerateFiles(assets, "*.cs", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();

        return files.Select(p => {
            cancellationToken.ThrowIfCancellationRequested();
            return ScanFile(root, p);
        }).ToArray();
    }

    private static CSharpScriptModel ScanFile(string root, string path)
    {
        var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
        var text = File.ReadAllText(path);
        var nsMatch = NamespaceRegex.Match(text);
        var ns = nsMatch.Success ? nsMatch.Groups[1].Value : null;
        var diagnostics = new List<Diagnostic>();
        var types = new List<CSharpTypeModel>();

        foreach (Match m in TypeRegex.Matches(text))
        {
            var kind = m.Groups["kind"].Value switch
            {
                "class" => CSharpTypeKind.Class,
                "struct" => CSharpTypeKind.Struct,
                "interface" => CSharpTypeKind.Interface,
                "enum" => CSharpTypeKind.Enum,
                _ => CSharpTypeKind.Unknown
            };

            var name = m.Groups["name"].Value;
            var baseText = m.Groups["base"].Success ? m.Groups["base"].Value.Trim() : null;
            var line = text.AsSpan(0, m.Index).Count('\n') + 1;
            // TypeRegex consumes leading attributes as part of the match, so m.Index
            // points at the first attribute rather than the declaration keyword. Use the
            // captured attribute group directly; otherwise [RequireComponent] is missed.
            var attrText = m.Groups["attrs"].Success ? m.Groups["attrs"].Value : string.Empty;

            var attrs = AttrRegex.Matches(attrText)
                .Select(x => x.Groups[1].Value.Trim())
                .Distinct(StringComparer.Ordinal).ToArray();

            var required = RequireRegex.Matches(attrText)
                .Select(x => x.Groups["name"].Value)
                .Distinct(StringComparer.Ordinal).ToArray();


            var bodyStart = text.IndexOf('{', m.Index);
            var bodyEnd = bodyStart >= 0 ? FindMatchingBrace(text, bodyStart) : -1;
            var body = bodyStart >= 0 && bodyEnd > bodyStart
                ? text[bodyStart..(bodyEnd + 1)] : string.Empty;

            var lifecycle = LifecycleRegex.Matches(body)
                .Select(x => x.Groups[1].Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            var serialized = SerializedRegex.Matches(body)
                .Cast<Match>()
                .Where(x =>
                    x.Groups["attrs"].Value.Contains("SerializeField", StringComparison.Ordinal) ||
                    x.Groups["attrs"].Value.Contains("SerializeReference", StringComparison.Ordinal) ||
                    x.Groups["access"].Value == "public")
                .Select(x => new SerializedFieldModel(
                    x.Groups["name"].Value,
                    x.Groups["type"].Value,
                    x.Groups["access"].Value == "private",
                    relative,
                    line))
                .Take(200)
                .ToArray();

            types.Add(new CSharpTypeModel(
                EntityId.FromStableKey($"{relative}:{name}"),
                name, kind, ns, baseText, attrs, lifecycle, required,
                serialized, relative, line));
        }

        if (types.Count == 0)
            diagnostics.Add(new Diagnostic(
                "CSHARP-TYPE-001", DiagnosticSeverity.Warning,
                "No top-level type declaration was detected by the lexical scanner.", relative));

        return new CSharpScriptModel(
            EntityId.FromStableKey(relative), relative, ns, types, diagnostics);
    }

    private static int FindMatchingBrace(string text, int start)
    {
        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }
            if (c == '"') { inString = true; continue; }
            if (c == '{') depth++;
            if (c == '}' && --depth == 0) return i;
        }
        return -1;
    }
}
