namespace UPA.Core;

public sealed record FootprintReport(long FilesRead, long FilesWritten, long NetworkBytes, long AllocatedBytes, int ApprovalCount);

public sealed record FootprintLimits(long MaxFilesRead, long MaxFilesWritten, long MaxNetworkBytes, long MaxAllocatedBytes, int MaxApprovals);

public static class FootprintPolicy
{
    public static IReadOnlyList<string> Validate(FootprintReport report, FootprintLimits limits)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(limits);
        var violations = new List<string>();
        if (report.FilesRead > limits.MaxFilesRead) violations.Add("Files read limit exceeded.");
        if (report.FilesWritten > limits.MaxFilesWritten) violations.Add("Files written limit exceeded.");
        if (report.NetworkBytes > limits.MaxNetworkBytes) violations.Add("Network limit exceeded.");
        if (report.AllocatedBytes > limits.MaxAllocatedBytes) violations.Add("Allocation limit exceeded.");
        if (report.ApprovalCount > limits.MaxApprovals) violations.Add("Approval interaction limit exceeded.");
        return violations;
    }
}
