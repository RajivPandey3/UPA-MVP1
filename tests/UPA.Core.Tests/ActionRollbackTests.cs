using System.Collections.Generic;
using UPA.Core;

namespace UPA.Core.Tests;

public sealed class ActionRollbackTests
{
    [Fact]
    public void RollsBackAppliedItemsInReverseOrder()
    {
        var results = new[]
        {
            new ActionExecutionResult("first", ActionResultStatus.Applied, "ok"),
            new ActionExecutionResult("skipped", ActionResultStatus.Skipped, "owner"),
            new ActionExecutionResult("second", ActionResultStatus.Applied, "ok")
        };
        var order = new List<string>();
        var rollback = ActionRollback.Rollback(results, id => { order.Add(id); return new ActionRollbackResult(id, true, "removed"); });
        Assert.Equal(new[] { "second", "first" }, order);
        Assert.All(rollback, result => Assert.True(result.RolledBack));
    }
}
