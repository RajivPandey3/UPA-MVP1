using System.Text.RegularExpressions;
using UPA.Health;
using UPA.ProjectModel;

namespace UPA.Planning;

public sealed class IntentPlanner
{
    public UpaPlan BuildPlan(
        string intent,
        UnifiedProjectModel? projectModel = null,
        ArchitectureHealthReport? health = null)
    {
        if (string.IsNullOrWhiteSpace(intent))
            throw new ArgumentException("Intent cannot be empty.", nameof(intent));

        var actions = new List<PlanAction>();
        var inputs = new List<PlanInput>();
        var unknowns = new List<PlanUnknown>();

        var lower = intent.ToLowerInvariant();

        if (IntentGrammar.MentionsAny(intent, "scene", "hierarchy", "gameobject"))
        {
            actions.Add(Action(
                "inspect-scene",
                PlanActionKind.Inspect,
                "Scene/Hierarchy",
                "Inspect existing scene hierarchy before changing it.",
                [],
                PlanRisk.Low,
                0.96,
                false));

            if (IntentGrammar.MentionsAny(intent, "create", "make", "add"))
            {
                actions.Add(Action(
                    "create-gameobject",
                    PlanActionKind.Create,
                    "Scene/GameObject",
                    "Create requested GameObject structure after inspection.",
                    ["inspect-scene"],
                    PlanRisk.Medium,
                    0.88,
                    true));
            }
        }

        if (IntentGrammar.MentionsAny(intent, "script", "system", "manager", "controller"))
        {
            actions.Add(Action(
                "inspect-scripts",
                PlanActionKind.Inspect,
                "Scripts/Assemblies",
                "Inspect scripts, types and assembly boundaries relevant to the requested system.",
                [],
                PlanRisk.Low,
                0.95,
                false));

            if (IntentGrammar.MentionsAny(intent, "create", "make", "add", "build"))
            {
                actions.Add(Action(
                    "configure-script-system",
                    PlanActionKind.Configure,
                    "Scripts/Architecture",
                    "Prepare the requested script/system configuration.",
                    ["inspect-scripts"],
                    PlanRisk.Medium,
                    0.84,
                    true));
            }
        }

        if (IntentGrammar.MentionsAny(intent, "material", "texture", "model", "mesh", "audio", "animation"))
        {
            actions.Add(Action(
                "inspect-assets",
                PlanActionKind.Inspect,
                "Assets",
                "Inspect available production assets and their references.",
                [],
                PlanRisk.Low,
                0.94,
                false));

            if (IntentGrammar.MentionsAny(intent, "create", "make", "generate"))
            {
                actions.Add(Action(
                    "asset-placeholder",
                    PlanActionKind.GeneratePlaceholder,
                    "Assets",
                    "Generate a clearly marked placeholder where exact artwork/source material is unavailable.",
                    ["inspect-assets"],
                    PlanRisk.Low,
                    0.82,
                    false));
            }
        }

        if (IntentGrammar.MentionsAny(intent, "tag", "layer", "physics", "collider", "rigidbody", "component"))
        {
            actions.Add(Action(
                "inspect-components",
                PlanActionKind.Inspect,
                "Components/ProjectSettings",
                "Inspect existing components, tags, layers and relevant project configuration.",
                [],
                PlanRisk.Low,
                0.92,
                false));

            if (IntentGrammar.MentionsAny(intent, "add", "create", "set", "configure"))
            {
                actions.Add(Action(
                    "configure-components",
                    PlanActionKind.Configure,
                    "Components/ProjectSettings",
                    "Apply requested component/configuration changes only after explicit approval.",
                    ["inspect-components"],
                    PlanRisk.Medium,
                    0.83,
                    true));
            }
        }

        if (actions.Count == 0)
        {
            unknowns.Add(new PlanUnknown(
                "intent.target",
                "The request does not identify a recognized UPA target domain.",
                true,
                "Specify the target such as scene, GameObject, script, asset, prefab, component or project setting."));
        }

        if (IntentGrammar.MentionsAny(intent, "exact", "final", "aaa") &&
            IntentGrammar.MentionsAny(intent, "texture", "model", "art", "audio", "animation"))
        {
            unknowns.Add(new PlanUnknown(
                "production.asset",
                "Exact production asset was requested but no source asset is available in the intent.",
                false,
                "Use an explicit placeholder/generator until the real asset is supplied."));
        }

        if (health != null && health.Findings.Any(x => x.Severity >= FindingSeverity.Error))
        {
            actions.Insert(0, Action(
                "health-gate",
                PlanActionKind.Validate,
                "Project Health",
                "Resolve or explicitly acknowledge blocking health findings before mutation planning.",
                [],
                PlanRisk.High,
                0.98,
                true));
        }

        var normalized = TopologicalOrder(actions);
        var executable = normalized.Count > 0 &&
                         !unknowns.Any(x => x.Blocking) &&
                         normalized.All(x => x.Preconditions.All(p => !p.Blocking));

        return new UpaPlan(
            "plan-" + Guid.NewGuid().ToString("N")[..12],
            intent.Trim(),
            inputs,
            normalized,
            unknowns,
            normalized.Any(x => x.RequiresApproval) || unknowns.Count > 0,
            false, // MVP-1 never grants execution authority.
            IntentGrammar.Version);
    }

    private static PlanAction Action(
        string id,
        PlanActionKind kind,
        string target,
        string description,
        IReadOnlyList<string> dependsOn,
        PlanRisk risk,
        double confidence,
        bool approval)
        => new(
            id, kind, target, description, dependsOn,
            new[]
            {
                new PlanPrecondition(
                    "READ-MODEL-001",
                    "Relevant ProjectModel data must be available.",
                    true)
            },
            risk,
            confidence,
            approval,
            kind == PlanActionKind.GeneratePlaceholder);

    private static IReadOnlyList<PlanAction> TopologicalOrder(
        IReadOnlyList<PlanAction> actions)
    {
        var result = new List<PlanAction>();
        var remaining = actions.ToDictionary(x => x.Id, StringComparer.Ordinal);

        while (remaining.Count > 0)
        {
            var ready = remaining.Values
                .Where(x => x.DependsOn.All(d => result.Any(r => r.Id == d)))
                .OrderBy(x => x.Id, StringComparer.Ordinal)
                .FirstOrDefault();

            if (ready == null)
                throw new InvalidOperationException("Plan dependency cycle detected.");

            result.Add(ready);
            remaining.Remove(ready.Id);
        }

        return result;
    }
}
