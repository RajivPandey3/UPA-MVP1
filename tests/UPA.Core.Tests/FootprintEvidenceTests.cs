using System.Text.Json;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class FootprintEvidenceTests
{
    [Fact]
    public void SerializesFootprintViolationsForAudit()
    {
        using var document = JsonDocument.Parse(FootprintEvidence.Serialize(new FootprintReport(2, 0, 0, 0, 0), new FootprintLimits(1, 1, 1, 1, 1)));
        Assert.Single(document.RootElement.GetProperty("violations").EnumerateArray());
    }
}
