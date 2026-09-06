namespace UPA.Analysis.Tests;

internal static class PerformanceProfile
{
    public static string Name => Environment.GetEnvironmentVariable("UPA_PERF_PROFILE")?.Trim().ToLowerInvariant() ?? "small";
    public static int Files(int requested) => Math.Max(1, (int)Math.Round(requested * (Name switch { "large" => 1, "stress" => 5, _ => 0.05 })));
}
