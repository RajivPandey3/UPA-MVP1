using System.Text.Json;

namespace UPA.Core;

public sealed class AttachmentManifestStore
{
    private readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public void Save(string path, ProjectAttachmentManifest manifest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(manifest);
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(manifest, options));
        File.Move(tempPath, fullPath, true);
    }

    public ProjectAttachmentManifest Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Attachment manifest was not found.", fullPath);
        try
        {
            return JsonSerializer.Deserialize<ProjectAttachmentManifest>(File.ReadAllText(fullPath), options)
                ?? throw new InvalidDataException("Attachment manifest is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Attachment manifest is not valid JSON.", exception);
        }
    }
}
