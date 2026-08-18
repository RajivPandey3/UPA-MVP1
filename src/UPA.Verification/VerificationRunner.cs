namespace UPA.Verification;

public sealed class VerificationRunner
{
    public VerificationReport Run(
        IReadOnlyList<VerificationCase> cases)
    {
        var results = new List<VerificationResult>();

        foreach (var testCase in cases)
        {
            try
            {
                results.Add(testCase.Run());
            }
            catch (Exception ex)
            {
                results.Add(new VerificationResult(
                    testCase.Id,
                    VerificationStatus.Fail,
                    ex.Message));
            }
        }

        return new VerificationReport(
            results,
            results.Count(x => x.Status == VerificationStatus.Pass),
            results.Count(x => x.Status == VerificationStatus.Fail),
            results.Count(x => x.Status == VerificationStatus.Blocked));
    }
}
