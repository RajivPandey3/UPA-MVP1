using UPA.Core;

namespace UPA.Core.Tests;

public sealed class FootprintPolicyTests
{
    [Fact]
    public void ReportsRuntimeAndPermissionBudgetViolations()
    {
        var violations = FootprintPolicy.Validate(new FootprintReport(10, 2, 100, 1000, 3), new FootprintLimits(5, 1, 50, 500, 2));
        Assert.Equal(5, violations.Count);
    }

    [Fact]
    public void AcceptsReportWithinDeclaredBudget()
    {
        Assert.Empty(FootprintPolicy.Validate(new FootprintReport(1, 1, 0, 10, 1), new FootprintLimits(5, 2, 100, 100, 2)));
    }
}
