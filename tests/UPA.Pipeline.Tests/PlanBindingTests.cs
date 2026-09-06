using UPA.Core;
using UPA.Planning;

namespace UPA.Pipeline.Tests;

public sealed class PlanBindingTests
{
    [Theory]
    [InlineData("Create", "Controller")]
    [InlineData("Create", "Material")]
    [InlineData("Make", "Player")]
    [InlineData("Add", "Player")]
    public void SupportedSyntaxBindsNamesWithoutTreatingThemAsCommands(string verb, string name)
    {
        var plan = new IntentPlanner().BuildPlan($"{verb} a GameObject named {name} with a Rigidbody in the scene.");
        var binder = new UnityBatchPlanBinder(Environment.ProcessPath!, "Assets/Proof.unity");
        var scan = new ScanResult(EntityId.New(), DateTimeOffset.UtcNow, Array.Empty<Diagnostic>()) { ProjectRoot = Path.GetTempPath() };
        var transaction = binder.Bind(plan, scan);
        Assert.Contains(name, transaction.Preview);
    }

    [Fact]
    public void RemovingValidatedActionsMustPreventUnityBinding()
    {
        var plan = new IntentPlanner().BuildPlan("Create a GameObject named Player with a Rigidbody in the scene.");
        var altered = plan with { Actions = Array.Empty<PlanAction>() };
        var binder = new UnityBatchPlanBinder(Environment.ProcessPath!, "Assets/Proof.unity");
        var scan = new ScanResult(EntityId.New(), DateTimeOffset.UtcNow, Array.Empty<Diagnostic>()) { ProjectRoot = Path.GetTempPath() };
        Assert.Throws<InvalidOperationException>(() => binder.Bind(altered, scan));
    }

    [Fact]
    public void RemovingComponentActionMustPreventUnityBinding()
    {
        var plan = new IntentPlanner().BuildPlan("Create a GameObject named Player with a Rigidbody in the scene.");
        var altered = plan with { Actions = plan.Actions.Where(action => action.Id != "configure-components").ToArray() };
        var binder = new UnityBatchPlanBinder(Environment.ProcessPath!, "Assets/Proof.unity");
        var scan = new ScanResult(EntityId.New(), DateTimeOffset.UtcNow, Array.Empty<Diagnostic>()) { ProjectRoot = Path.GetTempPath() };
        Assert.Throws<InvalidOperationException>(() => binder.Bind(altered, scan));
    }
}
