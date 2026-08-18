namespace UPA.Verification;

public static class RegressionMatrix
{
    public static IReadOnlyList<string> RequiredScenarios =>
        new[]
        {
            "plan.valid.basic",
            "plan.invalid.missing-target",
            "target.exact-global-id",
            "target.ambiguous-name",
            "approval.missing",
            "approval.present",
            "preview.required",
            "parameter.missing-required",
            "parameter.type-mismatch",
            "executor.allowlist",
            "transaction.rollback",
            "audit.deterministic",
            "project-settings.high-risk",
            "importer.typed-property",
            "health.blocking",
            "successful-end-to-end"
        };
}
