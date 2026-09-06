namespace UPA.Core;

public enum EvidenceStatus
{
    Confirmed,
    Inferred,
    Unknown,
    Missing,
    Conflicted,
    Stale
}

public sealed record EvidenceFact
{
    public EvidenceFact(EntityId subject, string predicate, string value, EvidenceStatus status,
        decimal confidence, DateTimeOffset observedAt, string source)
    {
        if (string.IsNullOrWhiteSpace(predicate)) throw new ArgumentException("Predicate is required.", nameof(predicate));
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source is required.", nameof(source));
        if (confidence is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(confidence));
        Subject = subject; Predicate = predicate; Value = value; Status = status;
        Confidence = confidence; ObservedAt = observedAt; Source = source;
    }

    public EntityId Subject { get; }
    public string Predicate { get; }
    public string Value { get; }
    public EvidenceStatus Status { get; }
    public decimal Confidence { get; }
    public DateTimeOffset ObservedAt { get; }
    public string Source { get; }
}
