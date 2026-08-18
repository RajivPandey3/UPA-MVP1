using UPA.Governance;
using UPA.Planning;

namespace UPA.Governance.Tests;

public class PlanValidatorTests
{
    [Fact]
    public void ValidatorAcceptsCorrectDependencyOrder()
    {
        var plan = new IntentPlanner().BuildPlan(
            "Create a player GameObject in the scene.");

        var result = new PlanValidator().Validate(plan);

        Assert.True(result.IsValid);
        Assert.True(result.CanEnterApproval);
    }

    [Fact]
    public void ValidatorRejectsMissingDependency()
    {
        var action = new PlanAction(
            "create",
            PlanActionKind.Create,
            "GameObject",
            "Create object",
            new[] { "missing" },
            Array.Empty<PlanPrecondition>(),
            PlanRisk.Medium,
            0.9,
            true,
            false);

        var plan = new UpaPlan(
            "p", "test", Array.Empty<PlanInput>(),
            new[] { action }, Array.Empty<PlanUnknown>(),
            true, false, "1.0");

        var result = new PlanValidator().Validate(plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, x => x.Code == "PLAN-003");
    }

    [Fact]
    public void PreviewNeverAuthorizesExecution()
    {
        var plan = new IntentPlanner().BuildPlan(
            "Create a player GameObject.");

        var validation = new PlanValidator().Validate(plan);
        var packet = new PreviewEngine().BuildApprovalPacket(plan, validation);

        Assert.False(packet.ExecutionAuthorized);
        Assert.False(GovernancePolicy.IsExecutionAuthorized(packet));
        Assert.NotEmpty(packet.Preview);
    }
}
