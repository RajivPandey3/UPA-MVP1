using System;
using System.Text.Json;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class PerformanceEvidenceTests
{
    [Fact]
    public void SerializesMachineReadableGateEvidence()
    {
        var measurement = new PerformanceMeasurement("small", 10, TimeSpan.FromSeconds(1), 10, false);
        var threshold = new PerformanceThreshold("small", TimeSpan.FromSeconds(2), 20);
        var gate = PerformanceGate.Evaluate(measurement, threshold);
        using var document = JsonDocument.Parse(PerformanceEvidence.Serialize(measurement, threshold, gate));
        Assert.Equal("small", document.RootElement.GetProperty("measurement").GetProperty("profile").GetString());
        Assert.True(document.RootElement.GetProperty("result").GetProperty("passed").GetBoolean());
    }
}
