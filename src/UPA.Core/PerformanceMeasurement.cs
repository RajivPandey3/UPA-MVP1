namespace UPA.Core;

public sealed record PerformanceMeasurement(
    string Profile,
    int Items,
    TimeSpan Duration,
    long AllocatedBytes,
    bool Cancelled)
{
    public double ItemsPerSecond => Duration.TotalSeconds <= 0 ? Items : Items / Duration.TotalSeconds;
}
