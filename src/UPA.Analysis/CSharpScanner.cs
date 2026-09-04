using System.Text.RegularExpressions;
using UPA.Core;

namespace UPA.Analysis;

public sealed record CSharpScanDelta(
    IReadOnlyList<CSharpScriptModel> UpdatedModels,
    IReadOnlyList<string> RemovedRelativePaths,
    bool IsColdScan
);

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

    private static readonly string[] LifecycleMethods =
        ["Awake", "OnEnable", "Start", "Update", "FixedUpdate", "LateUpdate",
         "OnDisable", "OnDestroy", "OnTriggerEnter", "OnCollisionEnter"];

    public IReadOnlyList<CSharpScriptModel> Scan(ScanContext context, CancellationToken cancellationToken = default)
    {
        return ScanAsync(context, cancellationToken).GetAwaiter().GetResult();
    }

    public async Task<IReadOnlyList<CSharpScriptModel>> ScanAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        var delta = await ScanDeltaAsync(context, cancellationToken);
        
        using var cache = new ProjectScannerCache(context.ProjectRoot);
        if (!cache.LoadIndex()) return delta.UpdatedModels;

        var result = new List<CSharpScriptModel>();
        foreach (var kvp in cache.Index)
        {
            var entry = kvp.Value;
            var model = cache.LoadModel(entry);
            if (model != null) result.Add(model);
        }
        return result;
    }

    public async Task<CSharpScanDelta> ScanDeltaAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(context.ProjectRoot);
        var assets = Path.Combine(root, "Assets");
        if (!Directory.Exists(assets))
            return new CSharpScanDelta(Array.Empty<CSharpScriptModel>(), Array.Empty<string>(), true);

        using var cache = new ProjectScannerCache(root);
        bool hasCache = cache.LoadIndex();

        var assetsDir = new DirectoryInfo(assets);
        var files = assetsDir.EnumerateFileSystemInfos("*.cs", SearchOption.AllDirectories)
            .OfType<FileInfo>()
            .ToArray();

        var unchanged = new List<CacheIndexEntry>();
        var updatedRecords = new List<UpdatedModelRecord>();
        var updatedModels = new List<CSharpScriptModel>();
        var removedPaths = new List<string>();
        var currentFiles = new HashSet<string>(StringComparer.Ordinal);
        
        int batchCount = 0;
        const int BatchSize = 500;

        foreach (var info in files)
        {
            if (++batchCount % BatchSize == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }
            
            var path = info.FullName;
            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            currentFiles.Add(relative);

            if (hasCache && cache.Index.TryGetValue(relative, out var entry))
            {
                if (entry.FileLength == info.Length && entry.LastWriteTimeUtcTicks == info.LastWriteTimeUtc.Ticks)
                {
                    unchanged.Add(entry);
                    continue;
                }

                ulong hash = ProjectScannerCache.ComputeHash(path);
                if (entry.ContentHash == hash)
                {
                    unchanged.Add(entry);
                    continue;
                }
            }

            var newModel = ScanFile(root, path);
            ulong newHash = ProjectScannerCache.ComputeHash(path);
            updatedRecords.Add(new UpdatedModelRecord(relative, info.Length, info.LastWriteTimeUtc.Ticks, newHash, newModel));
            updatedModels.Add(newModel);
        }

        if (hasCache)
        {
            foreach (var key in cache.Index.Keys)
            {
                if (!currentFiles.Contains(key))
                    removedPaths.Add(key);
            }
        }

        if (!context.ReadOnly && (updatedRecords.Count > 0 || removedPaths.Count > 0 || !hasCache))
        {
            cache.CommitDelta(unchanged, updatedRecords);
        }

        return new CSharpScanDelta(updatedModels, removedPaths, !hasCache);
    }

    private static CSharpScriptModel ScanFile(string root, string path)
    {
        return CSharpFastParser.ParseFile(root, path);
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
