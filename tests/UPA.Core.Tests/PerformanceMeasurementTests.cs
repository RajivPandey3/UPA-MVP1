using System;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class PerformanceMeasurementTests
{
    [Fact]
    public void CalculatesThroughputWithoutHidingCancellation()
    {
        var measurement = new PerformanceMeasurement("small", 100, TimeSpan.FromSeconds(2), 1024, true);
        Assert.Equal(50, measurement.ItemsPerSecond);
        Assert.True(measurement.Cancelled);
        Assert.Equal("small", measurement.Profile);
    }
}
