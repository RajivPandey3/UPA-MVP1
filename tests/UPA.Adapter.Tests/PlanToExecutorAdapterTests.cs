using UPA.Adapter;

namespace UPA.Adapter.Tests;

public class PlanToExecutorAdapterTests
{
    [Fact]
    public void AdapterBindsKnownOperation()
    {
        var adapter = new PlanToExecutorAdapter(
            OperationBindingCatalog.CreateDefault());

        var plan = adapter.Bind(
            "plan-1",
            new[]
            {
                new CompiledOperationWithArguments(
                    "component.add_rigidbody",
                    new Dictionary<string, object?>
                    {
                        ["targetId"] = "GlobalObjectId_V1-abc"
                    },
                    Array.Empty<string>(),
                    0.95,
                    true)
            });

        Assert.True(plan.ReadyForPreview);
        Assert.False(plan.ReadyForExecution);
        Assert.Single(plan.Batches);
    }

    [Fact]
    public void AdapterRejectsMissingRequiredParameter()
    {
        var adapter = new PlanToExecutorAdapter(
            OperationBindingCatalog.CreateDefault());

        var plan = adapter.Bind(
            "plan-1",
            new[]
            {
                new CompiledOperationWithArguments(
                    "component.add_rigidbody",
                    new Dictionary<string, object?>(),
                    Array.Empty<string>(),
                    0.95,
                    true)
            });

        Assert.False(plan.ReadyForPreview);
        Assert.Contains(
            plan.Issues,
            x => x.Code == "ADAPTER-PARAM-001");
    }

    [Fact]
    public void AdapterRejectsUnknownOperation()
    {
        var adapter = new PlanToExecutorAdapter(
            OperationBindingCatalog.CreateDefault());

        var plan = adapter.Bind(
            "plan-1",
            new[]
            {
                new CompiledOperationWithArguments(
                    "unknown.operation",
                    new Dictionary<string, object?>(),
                    Array.Empty<string>(),
                    0.5,
                    true)
            });

        Assert.False(plan.ReadyForPreview);
        Assert.Contains(
            plan.Issues,
            x => x.Code == "ADAPTER-BIND-001");
    }
}
