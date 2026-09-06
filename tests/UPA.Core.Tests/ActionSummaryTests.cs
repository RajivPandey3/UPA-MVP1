using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ActionSummaryTests
{
    [Fact]
    public void SummarizesBatchForDeveloper()
    {
        var summary = ActionSummary.Create(new[]
        {
            new ActionExecutionResult("a", ActionResultStatus.Applied, "ok"),
            new ActionExecutionResult("b", ActionResultStatus.Failed, "error"),
            new ActionExecutionResult("c", ActionResultStatus.Skipped, "owner")
        });
        Assert.Equal(1, summary.Applied);
        Assert.Equal(1, summary.Skipped);
        Assert.Equal(1, summary.Failed);
        Assert.True(summary.RollbackRequired);
        Assert.Contains("rollback", summary.NextAction);
    }
}
