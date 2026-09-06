using System.Text.Json;

namespace UPA.Core;

public static class FootprintEvidence
{
    public static string Serialize(FootprintReport report, FootprintLimits limits)
    {
        var violations = FootprintPolicy.Validate(report, limits);
        return JsonSerializer.Serialize(new { report, limits, violations }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
