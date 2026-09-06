using System.Security.Cryptography;

namespace UPA.Core;

public sealed class AttachmentOwnershipGuard
{
    public IReadOnlyList<string> GetRemovableFiles(string projectRoot, ProjectAttachmentManifest manifest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(manifest);
        var root = Path.GetFullPath(projectRoot);
        var removable = new List<string>();
        foreach (var ownedFile in manifest.OwnedFiles)
        {
            var path = Path.GetFullPath(Path.Combine(root, ownedFile.RelativePath));
            if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Manifest path escapes the project root.");
            if (!File.Exists(path)) continue;
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            if (hash.Equals(ownedFile.Sha256, StringComparison.OrdinalIgnoreCase)) removable.Add(path);
        }
        return removable;
    }
}
