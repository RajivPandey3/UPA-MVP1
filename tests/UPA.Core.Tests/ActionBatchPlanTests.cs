using System;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ActionBatchPlanTests
{
    [Fact]
    public void RequiresApprovalBeforeExecution()
    {
        var finding = ActionDecisionPolicy.Classify("f1", "Safe", EvidenceStatus.Confirmed, true, true);
        var plan = ActionBatchPlan.Create("plan-1", new[] { finding }, "developer");
        Assert.Throws<InvalidOperationException>(() => plan.EnsureExecutable());
        plan.Approve().EnsureExecutable();
    }

    [Fact]
    public void UnknownFindingAlwaysBlocksExecution()
    {
        var finding = ActionDecisionPolicy.Classify("f1", "Unknown", EvidenceStatus.Unknown, true, true);
        Assert.Throws<InvalidOperationException>(() => ActionBatchPlan.Create("plan-2", new[] { finding }, "developer").Approve().EnsureExecutable());
    }
}
