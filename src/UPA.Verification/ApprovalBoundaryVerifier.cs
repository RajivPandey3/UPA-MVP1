namespace UPA.Verification;

public static class ApprovalBoundaryVerifier
{
    public static VerificationResult Verify(
        string id,
        bool previewAccepted,
        bool approvalProvided,
        bool executionAttempted,
        bool executionShouldBeAllowed)
    {
        if (executionAttempted &&
            !approvalProvided &&
            executionShouldBeAllowed)
        {
            return new VerificationResult(
                id,
                VerificationStatus.Fail,
                "Execution was attempted without explicit approval.");
        }

        if (!previewAccepted && approvalProvided)
        {
            return new VerificationResult(
                id,
                VerificationStatus.Fail,
                "Approval must not bypass preview.");
        }

        if (!approvalProvided && executionShouldBeAllowed)
        {
            return new VerificationResult(
                id,
                VerificationStatus.Fail,
                "Execution authorization exists without approval.");
        }

        return new VerificationResult(
            id,
            VerificationStatus.Pass,
            "Approval boundary preserved.");
    }
}
