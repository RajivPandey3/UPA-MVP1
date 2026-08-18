namespace UPA.Planning;

public static class IntentGrammar
{
    public const string Version = "1.0";

    public static readonly IReadOnlyDictionary<string, PlanActionKind> Verbs =
        new Dictionary<string, PlanActionKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["inspect"] = PlanActionKind.Inspect,
            ["scan"] = PlanActionKind.Inspect,
            ["create"] = PlanActionKind.Create,
            ["make"] = PlanActionKind.Create,
            ["add"] = PlanActionKind.Create,
            ["configure"] = PlanActionKind.Configure,
            ["setup"] = PlanActionKind.Configure,
            ["set"] = PlanActionKind.Configure,
            ["connect"] = PlanActionKind.Link,
            ["link"] = PlanActionKind.Link,
            ["generate"] = PlanActionKind.GeneratePlaceholder,
            ["validate"] = PlanActionKind.Validate,
            ["check"] = PlanActionKind.Validate,
            ["report"] = PlanActionKind.Report
        };

    public static bool MentionsAny(string intent, params string[] terms)
        => terms.Any(x => intent.Contains(x, StringComparison.OrdinalIgnoreCase));
}
