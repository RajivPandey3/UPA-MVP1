namespace UPA.Verification;

public enum VerificationStatus
{
    Pass,
    Fail,
    Blocked,
    Skipped
}

public sealed record VerificationCase(
    string Id,
    string Description,
    Func<VerificationResult> Run);

public sealed record VerificationResult(
    string Id,
    VerificationStatus Status,
    string Message);

public sealed record VerificationReport(
    IReadOnlyList<VerificationResult> Results,
    int Passed,
    int Failed,
    int Blocked)
{
    public bool IsGreen => Failed == 0 && Blocked == 0;
}
