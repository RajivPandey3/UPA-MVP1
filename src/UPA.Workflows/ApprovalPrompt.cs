using UPA.Execution;

namespace UPA.Pipeline;


public static class ApprovalPrompt
{
    public static ApprovalToken? Read(ExecutionPreview preview, TextReader input, TextWriter output)
    {
        output.WriteLine("Requested: " + preview.Intent);
        output.WriteLine("Changes: " + preview.Changes);
        output.WriteLine("Type APPROVE to apply these changes, or anything else to cancel:");
        output.Flush();
        if (!string.Equals(input.ReadLine(), "APPROVE", StringComparison.Ordinal)) return null;
        return new ApprovalToken(preview.PlanId, Environment.UserName, DateTimeOffset.UtcNow, true)
        {
            ContentHash = preview.ContentHash
        };
    }
}
