namespace UPA.Core;

public enum KnowledgeDimension
{
    Project,
    Hierarchy,
    Inspector,
    Relationship
}

public enum RelationshipKind
{
    Contains,
    ParentOf,
    Uses,
    References,
    ConfiguredBy,
    DependsOn,
    AttachedTo
}

public sealed record ProjectKnowledgeNode
{
    public ProjectKnowledgeNode(EntityId id, KnowledgeDimension dimension, string nativeIdentity,
        string type, string location, EvidenceStatus status, DateTimeOffset observedAt)
    {
        if (string.IsNullOrWhiteSpace(nativeIdentity)) throw new ArgumentException("Native identity is required.", nameof(nativeIdentity));
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Type is required.", nameof(type));
        if (string.IsNullOrWhiteSpace(location)) throw new ArgumentException("Location is required.", nameof(location));
        Id = id; Dimension = dimension; NativeIdentity = nativeIdentity; Type = type;
        Location = location; Status = status; ObservedAt = observedAt;
    }
    public EntityId Id { get; }
    public KnowledgeDimension Dimension { get; }
    public string NativeIdentity { get; }
    public string Type { get; }
    public string Location { get; }
    public EvidenceStatus Status { get; }
    public DateTimeOffset ObservedAt { get; }
}

public sealed record ProjectKnowledgeEdge(
    EntityId From,
    EntityId To,
    RelationshipKind Kind,
    EvidenceStatus Status,
    DateTimeOffset ObservedAt,
    string Source);
