namespace UPA.Core;

public sealed record PerformanceThreshold(string Profile, TimeSpan MaxDuration, long MaxAllocatedBytes);

public sealed record PerformanceGateResult(bool Passed, IReadOnlyList<string> Findings);

public static class PerformanceGate
{
    public static PerformanceGateResult Evaluate(PerformanceMeasurement measurement, PerformanceThreshold threshold)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        ArgumentNullException.ThrowIfNull(threshold);
        var findings = new List<string>();
        if (!string.Equals(measurement.Profile, threshold.Profile, StringComparison.OrdinalIgnoreCase)) findings.Add("Profile mismatch.");
        if (measurement.Duration > threshold.MaxDuration) findings.Add("Duration threshold exceeded.");
        if (measurement.AllocatedBytes > threshold.MaxAllocatedBytes) findings.Add("Allocation threshold exceeded.");
        if (measurement.Cancelled) findings.Add("Measurement was cancelled.");
        return new PerformanceGateResult(findings.Count == 0, findings);
    }
}
