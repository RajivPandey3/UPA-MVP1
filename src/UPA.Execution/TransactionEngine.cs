namespace UPA.Execution;

public sealed class TransactionEngine
{
    private readonly PathSandbox _sandbox;

    public TransactionEngine(string sandboxRoot)
    {
        _sandbox = new PathSandbox(sandboxRoot);
    }

    public TransactionResult Execute(
        string planId,
        ApprovalToken? approval,
        IReadOnlyList<MutationRequest> mutations,
        bool dryRun,
        IReadOnlyList<ExecutionPrecondition>? preconditions = null)
    {
        var audit = new List<AuditEntry>();
        var errors = new List<string>();
        var snapshots = new List<FileSnapshot>();

        audit.Add(new(
            DateTimeOffset.UtcNow,
            planId,
            "TransactionStarted",
            dryRun ? "Dry-run" : "Mutation execution"));

        if (!dryRun &&
            (approval is null ||
             !approval.ExplicitlyApproved ||
             !string.Equals(approval.PlanId, planId, StringComparison.Ordinal)))
        {
            errors.Add("Explicit approval token for this plan is required.");
            audit.Add(new(
                DateTimeOffset.UtcNow,
                planId,
                "Rejected",
                "Missing or invalid approval token."));
            return new TransactionResult(false, false, false, audit, errors);
        }

        foreach (var precondition in preconditions ?? Array.Empty<ExecutionPrecondition>())
        {
            if (!precondition.Check())
            {
                errors.Add($"{precondition.Code}: {precondition.Description}");
                audit.Add(new(
                    DateTimeOffset.UtcNow,
                    planId,
                    "PreconditionFailed",
                    precondition.Code));
                return new TransactionResult(false, dryRun, false, audit, errors);
            }
        }

        foreach (var mutation in mutations)
        {
            try
            {
                var path = _sandbox.Resolve(mutation.RelativePath);
                snapshots.Add(Capture(mutation.RelativePath, path));

                ValidateMutation(mutation);

                audit.Add(new(
                    DateTimeOffset.UtcNow,
                    mutation.OperationId,
                    "MutationValidated",
                    mutation.Kind.ToString()));

                if (dryRun)
                    continue;

                Apply(mutation, path);

                audit.Add(new(
                    DateTimeOffset.UtcNow,
                    mutation.OperationId,
                    "MutationApplied",
                    mutation.RelativePath));
            }
            catch (Exception ex)
            {
                errors.Add($"{mutation.OperationId}: {ex.Message}");
                audit.Add(new(
                    DateTimeOffset.UtcNow,
                    mutation.OperationId,
                    "MutationFailed",
                    ex.Message));

                if (!dryRun)
                {
                    Rollback(snapshots);
                    audit.Add(new(
                        DateTimeOffset.UtcNow,
                        planId,
                        "RollbackCompleted",
                        "Transaction rolled back after failure."));

                    return new TransactionResult(
                        false, false, true, audit, errors);
                }
            }
        }

        audit.Add(new(
            DateTimeOffset.UtcNow,
            planId,
            "TransactionCompleted",
            dryRun ? "Dry-run completed." : "Committed."));

        return new TransactionResult(
            errors.Count == 0,
            dryRun,
            false,
            audit,
            errors);
    }

    private FileSnapshot Capture(string relative, string fullPath)
    {
        return File.Exists(fullPath)
            ? new FileSnapshot(relative, true, File.ReadAllText(fullPath))
            : new FileSnapshot(relative, false, null);
    }

    private static void ValidateMutation(MutationRequest mutation)
    {
        if (mutation.Kind is not
            (MutationKind.CreateTextFile or MutationKind.ReplaceTextFile))
            throw new InvalidOperationException("Mutation kind is not allowlisted.");

        if (mutation.RelativePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
            mutation.RelativePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
            mutation.RelativePath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) ||
            mutation.RelativePath.EndsWith(".bat", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Executable/script payloads are blocked by the MVP-1 sandbox.");
    }

    private static void Apply(MutationRequest mutation, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        if (mutation.Kind == MutationKind.CreateTextFile &&
            File.Exists(path))
            throw new IOException("Create operation refused because the file already exists.");

        File.WriteAllText(path, mutation.Content);
    }

    private void Rollback(IEnumerable<FileSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots.Reverse())
        {
            var path = _sandbox.Resolve(snapshot.RelativePath);

            if (snapshot.Exists)
                File.WriteAllText(path, snapshot.Content ?? string.Empty);
            else if (File.Exists(path))
                File.Delete(path);
        }
    }
}
