using UPA.Verification;

namespace UPA.Verification.Tests;

public class VerificationHarnessTests
{
    [Fact]
    public void MissingApprovalCannotExecute()
    {
        var result = ApprovalBoundaryVerifier.Verify(
            "approval-001",
            previewAccepted: true,
            approvalProvided: false,
            executionAttempted: false,
            executionShouldBeAllowed: true);

        Assert.Equal(VerificationStatus.Fail, result.Status);
    }

    [Fact]
    public void ApprovalDoesNotBypassPreview()
    {
        var result = ApprovalBoundaryVerifier.Verify(
            "approval-002",
            previewAccepted: false,
            approvalProvided: true,
            executionAttempted: false,
            executionShouldBeAllowed: true);

        Assert.Equal(VerificationStatus.Fail, result.Status);
    }

    [Fact]
    public void DeterminismPassesForIdenticalOutputs()
    {
        var result = DeterminismVerifier.Verify(
            "det-001",
            new[] { "A", "B", "C" },
            new[] { "A", "B", "C" });

        Assert.Equal(VerificationStatus.Pass, result.Status);
    }

    [Fact]
    public void RegressionMatrixContainsRollbackAndAmbiguity()
    {
        Assert.Contains(
            "transaction.rollback",
            RegressionMatrix.RequiredScenarios);

        Assert.Contains(
            "target.ambiguous-name",
            RegressionMatrix.RequiredScenarios);
    }

    [Fact]
    public void RunnerAggregatesFailures()
    {
        var runner = new VerificationRunner();

        var report = runner.Run(
            new[]
            {
                new VerificationCase(
                    "pass",
                    "passing case",
                    () => new VerificationResult(
                        "pass",
                        VerificationStatus.Pass,
                        "ok")),
                new VerificationCase(
                    "fail",
                    "failing case",
                    () => new VerificationResult(
                        "fail",
                        VerificationStatus.Fail,
                        "not ok"))
            });

        Assert.Equal(1, report.Passed);
        Assert.Equal(1, report.Failed);
        Assert.False(report.IsGreen);
    }
}
