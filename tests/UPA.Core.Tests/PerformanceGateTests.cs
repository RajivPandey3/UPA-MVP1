using System;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class PerformanceGateTests
{
    [Fact]
    public void PassesMeasurementWithinProfileThreshold()
    {
        var result = PerformanceGate.Evaluate(new PerformanceMeasurement("small", 10, TimeSpan.FromSeconds(1), 10, false), new PerformanceThreshold("small", TimeSpan.FromSeconds(2), 20));
        Assert.True(result.Passed);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void FailsCancelledOrOverBudgetMeasurement()
    {
        var result = PerformanceGate.Evaluate(new PerformanceMeasurement("small", 10, TimeSpan.FromSeconds(3), 30, true), new PerformanceThreshold("small", TimeSpan.FromSeconds(2), 20));
        Assert.False(result.Passed);
        Assert.Equal(3, result.Findings.Count);
    }
}
