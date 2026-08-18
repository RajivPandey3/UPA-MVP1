namespace UPA.Pipeline;

public static class PipelinePolicy
{
    public const string Version = "1.0";

    public static bool CanAutoApprove()
        => false;

    public static bool CanBypassPreview()
        => false;

    public static bool CanBypassValidation()
        => false;

    public static bool CanGrantExecutionAuthorityFromPlan()
        => false;

    public static string Explain()
        => "Every mutation path requires validation, preview, explicit approval, " +
           "allowlisted binding and transaction-aware execution.";
}
