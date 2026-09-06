namespace UPA.Core;

public static class ScanKnowledgeProjector
{
    public static IReadOnlyList<ProjectKnowledgeNode> Project(ScanResult scan)
    {
        ArgumentNullException.ThrowIfNull(scan);
        var nodes = new List<ProjectKnowledgeNode>
        {
            new(scan.ProjectId, KnowledgeDimension.Project,
                scan.ProjectId.Value, "Project", scan.ProjectRoot, EvidenceStatus.Confirmed, scan.CompletedAt)
        };
        foreach (var package in scan.Packages)
            nodes.Add(Node(scan.ProjectId, "package:" + package.Name, "Package", package.Name, scan.CompletedAt));
        foreach (var assembly in scan.Assemblies)
            nodes.Add(Node(scan.ProjectId, "assembly:" + assembly.Name, "Assembly", assembly.Path, scan.CompletedAt));
        foreach (var settingsPath in scan.ProjectSettingsFiles)
            nodes.Add(Node(scan.ProjectId, "settings:" + settingsPath, "ProjectSettings", settingsPath, scan.CompletedAt));
        foreach (var assetPath in scan.AssetPaths)
            nodes.Add(Node(scan.ProjectId, "asset:" + assetPath, "Asset", assetPath, scan.CompletedAt));
        return nodes;
    }

    private static ProjectKnowledgeNode Node(EntityId projectId, string identity, string type, string location, DateTimeOffset observedAt) =>
        new(EntityId.FromStableKey(projectId.Value + ":" + identity), KnowledgeDimension.Project,
            identity, type, location, EvidenceStatus.Confirmed, observedAt);
}
