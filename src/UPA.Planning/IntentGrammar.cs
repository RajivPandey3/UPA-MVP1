using System.Text.RegularExpressions;

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
        => terms.Any(term => Regex.IsMatch(intent, @"\b" + Regex.Escape(term) + @"\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

    public static bool RequiresConstraintClarification(string intent)
        => Regex.IsMatch(intent, @"\b(not|no|never|without|unless|except|avoid|only|don't|don’t|cannot|can't|can’t)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
