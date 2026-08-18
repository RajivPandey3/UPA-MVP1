using UPA.Planning;

namespace UPA.Planning.Tests;

public class IntentPlannerTests
{
    [Fact]
    public void PlannerCreatesInspectionBeforeMutation()
    {
        var plan = new IntentPlanner().BuildPlan(
            "Create a player GameObject with a Rigidbody and collider in the scene.");

        var inspect = plan.Actions.Single(x => x.Id == "inspect-scene");
        var create = plan.Actions.Single(x => x.Id == "create-gameobject");

        Assert.True(
            Array.IndexOf(plan.Actions.ToArray(), inspect) <
            Array.IndexOf(plan.Actions.ToArray(), create));

        Assert.Contains("inspect-scene", create.DependsOn);
        Assert.False(plan.Executable);
    }

    [Fact]
    public void PlannerFlagsUnknownIntent()
    {
        var plan = new IntentPlanner().BuildPlan("Do the thing.");

        Assert.Contains(plan.Unknowns, x => x.Blocking);
        Assert.True(plan.RequiresApproval);
    }

    [Fact]
    public void ExactMissingArtworkBecomesNonBlockingPlaceholderUnknown()
    {
        var plan = new IntentPlanner().BuildPlan(
            "Create the exact AAA final texture for this character.");

        Assert.Contains(plan.Unknowns, x => x.Key == "production.asset" && !x.Blocking);
        Assert.Contains(plan.Actions, x => x.Kind == PlanActionKind.GeneratePlaceholder);
    }
}
