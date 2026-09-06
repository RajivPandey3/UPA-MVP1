using UPA.Pipeline;

namespace UPA.Pipeline.Tests;

public class GovernedPipelineTests
{
    [Fact]
    public void PipelineStopsBeforeExecutionWithoutApproval()
    {
        var result = new GovernedPipeline().Start(
            "run-1",
            "Create player",
            projectModelAvailable: true,
            healthPassed: true,
            planValid: true,
            previewAccepted: true,
            explicitlyApproved: false,
            adapterReady: true,
            executorSucceeded: true);

        Assert.False(result.Success);
        Assert.Equal(PipelineStage.AwaitApproval, result.State.Stage);
        Assert.False(result.State.ExecutionAuthorized);
    }

    [Fact]
    public void PipelineBlocksWhenValidationFails()
    {
        var result = new GovernedPipeline().Start(
            "run-2",
            "Create player",
            true, true, false, true, true, true, true);

        Assert.False(result.Success);
        Assert.Equal(PipelineStage.Blocked, result.State.Stage);
        Assert.True(result.State.HasBlockingIssue);
    }

    [Fact]
    public void CallerFlagsCannotProveExecution()
    {
        var result = new GovernedPipeline().Start(
            "run-3",
            "Create player",
            true, true, true, true, true, true, true);

        Assert.False(result.Success);
        Assert.Equal(PipelineStage.Blocked, result.State.Stage);
        Assert.False(result.State.ExecutionAuthorized);
        Assert.DoesNotContain(
            result.State.Events,
            x => x.Code == "PIPE-100");
    }

    [Fact]
    public void PolicyForbidsBypass()
    {
        Assert.False(PipelinePolicy.CanAutoApprove());
        Assert.False(PipelinePolicy.CanBypassPreview());
        Assert.False(PipelinePolicy.CanBypassValidation());
        Assert.False(PipelinePolicy.CanGrantExecutionAuthorityFromPlan());
    }
}
