namespace UPA.Core;

public static class FreshnessPolicy
{
    public static EvidenceStatus Classify(DateTimeOffset observedAt, DateTimeOffset now, TimeSpan maxAge)
    {
        if (maxAge < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maxAge));
        if (observedAt > now) return EvidenceStatus.Conflicted;
        return now - observedAt > maxAge ? EvidenceStatus.Stale : EvidenceStatus.Confirmed;
    }
}
