namespace UPA.Verification;

public static class DeterminismVerifier
{
    public static VerificationResult Verify(
        string id,
        IReadOnlyList<string> first,
        IReadOnlyList<string> second)
    {
        if (!first.SequenceEqual(second, StringComparer.Ordinal))
        {
            return new VerificationResult(
                id,
                VerificationStatus.Fail,
                "Outputs differ across repeated runs.");
        }

        return new VerificationResult(
            id,
            VerificationStatus.Pass,
            "Outputs are deterministic.");
    }
}
