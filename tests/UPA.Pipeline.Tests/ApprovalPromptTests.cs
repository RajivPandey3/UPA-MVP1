namespace UPA.Pipeline.Tests;

public sealed class ApprovalPromptTests
{
    [Fact]
    public void PreviewShowsRequestBeforeAskingForApproval()
    {
        var output = new StringWriter();
        ApprovalPrompt.Read(new ExecutionPreview("plan", "Create Player", "Create Assets/Player.unity", "digest"),
            new StringReader("APPROVE"), output);
        Assert.True(output.ToString().IndexOf("Create Assets/Player.unity", StringComparison.Ordinal) <
            output.ToString().IndexOf("Type APPROVE", StringComparison.Ordinal));
    }
    [Theory]
    [InlineData("")]
    [InlineData("no")]
    [InlineData("yes")]
    public void DefaultAndNonApprovalInputCancel(string answer)
    {
        var output = new StringWriter();
        var token = ApprovalPrompt.Read(new ExecutionPreview("plan", "request", "Create Assets/Player.unity", "digest"),
            new StringReader(answer), output);
        Assert.Null(token);
        Assert.Contains("Create Assets/Player.unity", output.ToString());
    }

    [Fact]
    public void ApprovalUsesTheDisplayedPlanAndContentHash()
    {
        var output = new StringWriter();
        var token = ApprovalPrompt.Read(new ExecutionPreview("plan", "request", "Create Player with Rigidbody", "digest"),
            new StringReader("APPROVE"), output);
        Assert.NotNull(token);
        Assert.Equal("plan", token.PlanId);
        Assert.Equal("digest", token.ContentHash);
        Assert.Contains("Create Player with Rigidbody", output.ToString());
    }
}
