using System;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ProjectKnowledgeTests
{
    [Fact]
    public void PreservesNativeIdentityAndRelationship()
    {
        var project = new ProjectKnowledgeNode(EntityId.New(), KnowledgeDimension.Project, "project-guid", "UnityProject", "C:/game", EvidenceStatus.Confirmed, DateTimeOffset.UtcNow);
        var asset = new ProjectKnowledgeNode(EntityId.New(), KnowledgeDimension.Inspector, "asset-guid", "Prefab", "Assets/player.prefab", EvidenceStatus.Confirmed, DateTimeOffset.UtcNow);
        var edge = new ProjectKnowledgeEdge(project.Id, asset.Id, RelationshipKind.Contains, EvidenceStatus.Confirmed, DateTimeOffset.UtcNow, "scanner");

        Assert.Equal("project-guid", project.NativeIdentity);
        Assert.Equal(project.Id, edge.From);
        Assert.Equal(RelationshipKind.Contains, edge.Kind);
    }

    [Fact]
    public void RejectsNodeWithoutNativeIdentity()
    {
        Assert.Throws<ArgumentException>(() => new ProjectKnowledgeNode(EntityId.New(), KnowledgeDimension.Project, "", "Project", "root", EvidenceStatus.Unknown, DateTimeOffset.UtcNow));
    }
}
