using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ActionBatchOutcomeTests
{
    [Fact]
    public void FlagsPartialFailureExplicitly()
    {
        var outcome = ActionBatchOutcome.From(new[]
        {
            new ActionExecutionResult("a", ActionResultStatus.Applied, "ok"),
            new ActionExecutionResult("b", ActionResultStatus.Failed, "error")
        });
        Assert.False(outcome.Succeeded);
        Assert.True(outcome.PartialFailure);
    }

    [Fact]
    public void EmptyOrSkippedBatchIsNotPartialFailure()
    {
        var outcome = ActionBatchOutcome.From(new[] { new ActionExecutionResult("a", ActionResultStatus.Skipped, "owner") });
        Assert.True(outcome.Succeeded);
        Assert.False(outcome.PartialFailure);
    }
}
