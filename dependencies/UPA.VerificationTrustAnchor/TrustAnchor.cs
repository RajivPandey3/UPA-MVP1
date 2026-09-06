using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace UPA.VerificationTrustAnchor;

public static class CanonicalHash
{
    public static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    public static string Join(params object?[] values) =>
        string.Join("|", values.Select(v => v switch
        {
            null => "",
            DateTimeOffset dto => dto.ToUniversalTime().ToString("O"),
            IEnumerable<string> list => string.Join("|", list),
            _ => v.ToString() ?? ""
        }));
}

public sealed record ChainRootRegistryEntry(
    string ChainRootCertificateId,
    string BundleId,
    string BundleFingerprint,
    string FirstRegistryCertificateId,
    string LastRegistryCertificateId,
    int ChainLength,
    string ChainFingerprint,
    string ChainRootFingerprint,
    string CertificateHash,
    DateTimeOffset CertifiedUtc);

public sealed record ChainRootLookup(
    bool Found,
    ChainRootRegistryEntry? Latest,
    IReadOnlyList<ChainRootRegistryEntry> Candidates);

public sealed record RegistryVerification(
    bool Valid,
    IReadOnlyList<string> Errors,
    string RegistryFingerprint);

public sealed class ChainRootRegistry
{
    private readonly Dictionary<string, ChainRootRegistryEntry> _entries = new(StringComparer.Ordinal);

    public IReadOnlyList<ChainRootRegistryEntry> Entries =>
        new ReadOnlyCollection<ChainRootRegistryEntry>(
            _entries.Values
                .OrderBy(x => x.BundleId, StringComparer.Ordinal)
                .ThenByDescending(x => x.CertifiedUtc)
                .ThenBy(x => x.ChainRootCertificateId, StringComparer.Ordinal)
                .ToList());

    public void Register(ChainRootRegistryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(entry.ChainRootCertificateId))
            throw new InvalidOperationException("Chain-root certificate ID is required.");

        if (_entries.ContainsKey(entry.ChainRootCertificateId))
            throw new InvalidOperationException("Chain-root certificate ID already registered.");

        if (_entries.Values.Any(x => x.ChainRootFingerprint == entry.ChainRootFingerprint))
            throw new InvalidOperationException("Chain-root fingerprint already registered.");

        _entries.Add(entry.ChainRootCertificateId, entry);
    }

    public ChainRootLookup Lookup(string bundleId)
    {
        var candidates = Entries.Where(x => x.BundleId == bundleId).ToList();
        return new ChainRootLookup(candidates.Count > 0, candidates.FirstOrDefault(), candidates);
    }

    public ChainRootRegistryEntry? ById(string certificateId) =>
        _entries.TryGetValue(certificateId, out var value) ? value : null;

    public string Fingerprint() => CanonicalHash.Sha256(string.Join(
        "\n",
        Entries.Select(x => CanonicalHash.Join(
            x.ChainRootCertificateId, x.BundleId, x.BundleFingerprint,
            x.FirstRegistryCertificateId, x.LastRegistryCertificateId,
            x.ChainLength, x.ChainFingerprint, x.ChainRootFingerprint,
            x.CertificateHash, x.CertifiedUtc))));
}

public sealed record RegistryCertificateInput(
    string BundleId,
    string BundleFingerprint,
    int ChainRootCount,
    string FirstChainRootCertificateId,
    string FirstChainRootFingerprint,
    string LatestChainRootCertificateId,
    string LatestChainRootFingerprint,
    IReadOnlyList<string> OrderedChainRootCertificateIds,
    string RegistryFingerprint);

public sealed record RegistryCertificate(
    string CertificateId,
    RegistryCertificateInput Input,
    DateTimeOffset CertifiedUtc,
    string RegistryCertificateFingerprint,
    string CertificateHash);

public static class RegistryCertificateFactory
{
    public static RegistryCertificate Create(
        string certificateId,
        RegistryCertificateInput input,
        DateTimeOffset certifiedUtc)
    {
        Validate(input);

        var fingerprint = Fingerprint(input);
        var draft = new RegistryCertificate(
            certificateId, input, certifiedUtc, fingerprint, "");

        return draft with { CertificateHash = CertificateHash(draft) };
    }

    public static string Fingerprint(RegistryCertificateInput input) =>
        CanonicalHash.Sha256(CanonicalHash.Join(
            input.BundleId, input.BundleFingerprint, input.ChainRootCount,
            input.FirstChainRootCertificateId, input.FirstChainRootFingerprint,
            input.LatestChainRootCertificateId, input.LatestChainRootFingerprint,
            input.OrderedChainRootCertificateIds, input.RegistryFingerprint));

    public static string CertificateHash(RegistryCertificate certificate) =>
        CanonicalHash.Sha256(CanonicalHash.Join(
            certificate.CertificateId,
            certificate.Input.BundleId,
            certificate.Input.BundleFingerprint,
            certificate.Input.ChainRootCount,
            certificate.Input.FirstChainRootCertificateId,
            certificate.Input.LatestChainRootCertificateId,
            certificate.Input.OrderedChainRootCertificateIds,
            certificate.Input.RegistryFingerprint,
            certificate.RegistryCertificateFingerprint,
            certificate.CertifiedUtc));

    private static void Validate(RegistryCertificateInput input)
    {
        if (string.IsNullOrWhiteSpace(input.BundleId) ||
            string.IsNullOrWhiteSpace(input.BundleFingerprint))
            throw new InvalidOperationException("Bundle identity is required.");

        if (input.ChainRootCount <= 0 ||
            input.OrderedChainRootCertificateIds.Count != input.ChainRootCount)
            throw new InvalidOperationException("Chain-root count mismatch.");

        if (input.FirstChainRootCertificateId != input.OrderedChainRootCertificateIds[0] ||
            input.LatestChainRootCertificateId != input.OrderedChainRootCertificateIds[^1])
            throw new InvalidOperationException("Registry endpoint mismatch.");

        if (string.IsNullOrWhiteSpace(input.RegistryFingerprint))
            throw new InvalidOperationException("Registry fingerprint is required.");

        if (input.OrderedChainRootCertificateIds.Distinct(StringComparer.Ordinal).Count() !=
            input.OrderedChainRootCertificateIds.Count)
            throw new InvalidOperationException("Duplicate root IDs are not allowed.");
    }
}

public sealed record CertificateChainEntry(
    string EntryId,
    string BundleId,
    string BundleFingerprint,
    long Sequence,
    string RegistryCertificateId,
    string RegistryCertificateHash,
    string RegistryCertificateFingerprint,
    string? PreviousRegistryCertificateId,
    string? PreviousRegistryCertificateHash,
    DateTimeOffset CertifiedUtc);

public sealed record CertificateChainVerification(
    bool Valid,
    IReadOnlyList<string> Errors,
    string ChainFingerprint);

public sealed class RegistryCertificateChain
{
    private readonly Dictionary<string, CertificateChainEntry> _entries = new(StringComparer.Ordinal);

    public IReadOnlyList<CertificateChainEntry> Entries =>
        new ReadOnlyCollection<CertificateChainEntry>(
            _entries.Values
                .OrderBy(x => x.BundleId, StringComparer.Ordinal)
                .ThenBy(x => x.Sequence)
                .ThenBy(x => x.EntryId, StringComparer.Ordinal)
                .ToList());

    public void Register(CertificateChainEntry entry)
    {
        if (_entries.ContainsKey(entry.EntryId))
            throw new InvalidOperationException("Chain entry already registered.");

        if (_entries.Values.Any(x => x.RegistryCertificateId == entry.RegistryCertificateId))
            throw new InvalidOperationException("Registry certificate already chained.");

        if (_entries.Values.Any(x => x.RegistryCertificateFingerprint ==
                                      entry.RegistryCertificateFingerprint))
            throw new InvalidOperationException("Registry certificate fingerprint already chained.");

        _entries.Add(entry.EntryId, entry);
    }

    public string Fingerprint() => CanonicalHash.Sha256(string.Join(
        "\n",
        Entries.Select(x => CanonicalHash.Join(
            x.EntryId, x.BundleId, x.BundleFingerprint, x.Sequence,
            x.RegistryCertificateId, x.RegistryCertificateHash,
            x.RegistryCertificateFingerprint,
            x.PreviousRegistryCertificateId,
            x.PreviousRegistryCertificateHash,
            x.CertifiedUtc))));

    public CertificateChainVerification Verify()
    {
        var errors = new List<string>();

        foreach (var group in Entries.GroupBy(x => x.BundleId, StringComparer.Ordinal))
        {
            var ordered = group.OrderBy(x => x.Sequence)
                               .ThenBy(x => x.EntryId, StringComparer.Ordinal)
                               .ToArray();

            var sequences = new HashSet<long>();

            for (var i = 0; i < ordered.Length; i++)
            {
                var current = ordered[i];

                if (!sequences.Add(current.Sequence))
                    errors.Add($"Duplicate sequence: {current.Sequence}.");

                if (string.IsNullOrWhiteSpace(current.RegistryCertificateId))
                    errors.Add($"Missing registry certificate ID: {current.EntryId}.");

                if (string.IsNullOrWhiteSpace(current.RegistryCertificateHash))
                    errors.Add($"Missing registry certificate hash: {current.EntryId}.");

                if (string.IsNullOrWhiteSpace(current.RegistryCertificateFingerprint))
                    errors.Add($"Missing registry certificate fingerprint: {current.EntryId}.");

                if (i == 0)
                {
                    if (!string.IsNullOrWhiteSpace(current.PreviousRegistryCertificateId) ||
                        !string.IsNullOrWhiteSpace(current.PreviousRegistryCertificateHash))
                        errors.Add($"First chain entry has unexpected predecessor: {current.EntryId}.");
                    continue;
                }

                var previous = ordered[i - 1];

                if (current.Sequence != previous.Sequence + 1)
                    errors.Add($"Sequence gap: {previous.Sequence}->{current.Sequence}.");

                if (current.BundleFingerprint != previous.BundleFingerprint)
                    errors.Add($"Bundle fingerprint break: {current.EntryId}.");

                if (current.PreviousRegistryCertificateId != previous.RegistryCertificateId)
                    errors.Add($"Previous certificate ID break: {current.EntryId}.");

                if (current.PreviousRegistryCertificateHash != previous.RegistryCertificateHash)
                    errors.Add($"Previous certificate hash break: {current.EntryId}.");
            }
        }

        return new CertificateChainVerification(errors.Count == 0, errors, Fingerprint());
    }
}

public sealed record ChainRootInput(
    string BundleId,
    string BundleFingerprint,
    int ChainLength,
    string FirstRegistryCertificateId,
    string FirstRegistryCertificateHash,
    string LastRegistryCertificateId,
    string LastRegistryCertificateHash,
    IReadOnlyList<string> OrderedRegistryCertificateIds,
    string ChainFingerprint,
    bool ContinuityValid);

public sealed record ChainRootCertificate(
    string CertificateId,
    ChainRootInput Input,
    DateTimeOffset CertifiedUtc,
    string ChainRootFingerprint,
    string CertificateHash);

public static class ChainRootFactory
{
    public static ChainRootCertificate Create(
        string certificateId,
        ChainRootInput input,
        DateTimeOffset certifiedUtc)
    {
        Validate(input);

        var fingerprint = Fingerprint(input);
        var draft = new ChainRootCertificate(
            certificateId, input, certifiedUtc, fingerprint, "");

        return draft with { CertificateHash = CertificateHash(draft) };
    }

    public static string Fingerprint(ChainRootInput input) =>
        CanonicalHash.Sha256(CanonicalHash.Join(
            input.BundleId, input.BundleFingerprint, input.ChainLength,
            input.FirstRegistryCertificateId, input.FirstRegistryCertificateHash,
            input.LastRegistryCertificateId, input.LastRegistryCertificateHash,
            input.OrderedRegistryCertificateIds,
            input.ChainFingerprint, input.ContinuityValid));

    public static string CertificateHash(ChainRootCertificate certificate) =>
        CanonicalHash.Sha256(CanonicalHash.Join(
            certificate.CertificateId,
            certificate.Input.BundleId,
            certificate.Input.BundleFingerprint,
            certificate.Input.ChainLength,
            certificate.Input.FirstRegistryCertificateId,
            certificate.Input.LastRegistryCertificateId,
            certificate.Input.ChainFingerprint,
            certificate.ChainRootFingerprint,
            certificate.CertifiedUtc));

    public static VerificationResult Verify(ChainRootCertificate certificate)
    {
        var errors = new List<string>();
        var input = certificate.Input;

        if (certificate.ChainRootFingerprint != Fingerprint(input))
            errors.Add("Chain-root fingerprint mismatch.");

        if (certificate.CertificateHash != CertificateHash(certificate))
            errors.Add("Chain-root certificate hash mismatch.");

        if (input.ChainLength <= 0)
            errors.Add("Chain length must be positive.");

        if (input.OrderedRegistryCertificateIds.Count != input.ChainLength)
            errors.Add("Chain length mismatch.");

        if (!input.ContinuityValid)
            errors.Add("Continuity result is invalid.");

        if (input.OrderedRegistryCertificateIds.Count > 0)
        {
            if (input.FirstRegistryCertificateId != input.OrderedRegistryCertificateIds[0])
                errors.Add("First registry certificate mismatch.");

            if (input.LastRegistryCertificateId != input.OrderedRegistryCertificateIds[^1])
                errors.Add("Last registry certificate mismatch.");
        }

        if (input.OrderedRegistryCertificateIds.Distinct(StringComparer.Ordinal).Count() !=
            input.OrderedRegistryCertificateIds.Count)
            errors.Add("Duplicate registry certificates detected.");

        if (string.IsNullOrWhiteSpace(input.ChainFingerprint))
            errors.Add("Chain fingerprint missing.");

        return new VerificationResult(errors.Count == 0, errors);
    }

    private static void Validate(ChainRootInput input)
    {
        if (string.IsNullOrWhiteSpace(input.BundleId) ||
            string.IsNullOrWhiteSpace(input.BundleFingerprint))
            throw new InvalidOperationException("Bundle identity is required.");

        if (input.ChainLength <= 0 ||
            input.OrderedRegistryCertificateIds.Count != input.ChainLength)
            throw new InvalidOperationException("Chain length mismatch.");

        if (input.FirstRegistryCertificateId != input.OrderedRegistryCertificateIds[0] ||
            input.LastRegistryCertificateId != input.OrderedRegistryCertificateIds[^1])
            throw new InvalidOperationException("Chain endpoint mismatch.");

        if (!input.ContinuityValid)
            throw new InvalidOperationException("Invalid continuity cannot be rooted.");

        if (string.IsNullOrWhiteSpace(input.ChainFingerprint))
            throw new InvalidOperationException("Chain fingerprint is required.");

        if (input.OrderedRegistryCertificateIds.Distinct(StringComparer.Ordinal).Count() !=
            input.OrderedRegistryCertificateIds.Count)
            throw new InvalidOperationException("Duplicate registry certificates are not allowed.");
    }
}

public record VerificationResult(bool Valid, IReadOnlyList<string> Errors);

public static class GovernanceBoundary
{
    public const string EvidenceOnly =
        "Evidence/integrity verification only. No revocation, permits, " +
        "execution authorization, or Unity mutation.";
}
