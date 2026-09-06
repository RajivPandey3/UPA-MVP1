namespace UPA.Core;

public sealed record ScanConcurrencyPolicy
{
    public ScanConcurrencyPolicy(int maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    public int MaxDegreeOfParallelism { get; }
}
