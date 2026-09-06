using System;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ActionExecutionTests
{
    [Fact]
    public void ProducesPerItemEvidenceForApprovedBatch()
    {
        var finding = ActionDecisionPolicy.Classify("f1", "Safe", EvidenceStatus.Confirmed, true, true);
        var plan = ActionBatchPlan.Create("plan", new[] { finding }, "developer").Approve();
        var result = Assert.Single(ActionBatchExecutor.Execute(plan, item => new ActionExecutionResult(item.FindingId, ActionResultStatus.Applied, "verified")));
        Assert.Equal(ActionResultStatus.Applied, result.Status);
        Assert.Equal("verified", result.Evidence);
    }

    [Fact]
    public void DoesNotExecuteUnapprovedBatch()
    {
        var finding = ActionDecisionPolicy.Classify("f1", "Safe", EvidenceStatus.Confirmed, true, true);
        var plan = ActionBatchPlan.Create("plan", new[] { finding }, "developer");
        Assert.Throws<InvalidOperationException>(() => ActionBatchExecutor.Execute(plan, _ => throw new Exception("must not run")));
    }
}
