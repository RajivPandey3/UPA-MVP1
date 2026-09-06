using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UPA.Pipeline;


public sealed record RunRecord(string RunId, string Status, string ContentHash,
    IReadOnlyList<OutputExpectation> Outputs, IReadOnlyList<PipelineEvent> Events, string Detail);

public sealed class RunJournal
{
    private readonly string _path;
    private RunRecord _record;

    public RunJournal(string root, string runId, string contentHash, IReadOnlyList<OutputExpectation> outputs)
    {
        var directory = Path.Combine(root, ".upa", "runs");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(runId))) + ".json");
        _record = new RunRecord(runId, "Prepared", contentHash, outputs.ToArray(), Array.Empty<PipelineEvent>(), "");
        using var stream = new FileStream(_path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        JsonSerializer.Serialize(stream, _record);
        stream.Flush(flushToDisk: true);
    }

    public void Write(string status, IReadOnlyList<PipelineEvent> events, string detail = "")
    {
        _record = _record with { Status = status, Events = events.ToArray(), Detail = detail };
        var temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, _record);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, _path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static IReadOnlyList<RunRecord> Inspect(string root)
    {
        var directory = Path.Combine(root, ".upa", "runs");
        if (!Directory.Exists(directory)) return Array.Empty<RunRecord>();
        return Directory.GetFiles(directory, "*.json").OrderBy(path => path, StringComparer.Ordinal).Select(path => {
            try
            {
                var record = JsonSerializer.Deserialize<RunRecord>(File.ReadAllText(path));
                return record ?? throw new InvalidDataException("Empty record.");
            }
            catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException)
            {
                return new RunRecord(Path.GetFileNameWithoutExtension(path), "Corrupt", "",
                    Array.Empty<OutputExpectation>(), Array.Empty<PipelineEvent>(), exception.Message);
            }
        }).ToArray();
    }

    public static bool RequiresReview(RunRecord record)
        => record.Status is not ("Completed" or "RolledBack");
}
