using System.Text.Json;

namespace UPA.Core;

public static class PerformanceEvidence
{
    public static string Serialize(PerformanceMeasurement measurement, PerformanceThreshold threshold, PerformanceGateResult result)
    {
        ArgumentNullException.ThrowIfNull(measurement);
        ArgumentNullException.ThrowIfNull(threshold);
        ArgumentNullException.ThrowIfNull(result);
        return JsonSerializer.Serialize(new { measurement, threshold, result }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
