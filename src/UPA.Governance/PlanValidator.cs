using UPA.Planning;

namespace UPA.Governance;

public sealed class PlanValidator
{
    public PlanValidationResult Validate(UpaPlan plan)
    {
        var issues = new List<PlanValidationIssue>();

        if (plan.Actions.Count == 0)
            issues.Add(new("PLAN-001", ValidationSeverity.Error,
                "Plan contains no actions."));

        var ids = new HashSet<string>(StringComparer.Ordinal);

        foreach (var action in plan.Actions)
        {
            if (!ids.Add(action.Id))
                issues.Add(new("PLAN-002", ValidationSeverity.Error,
                    $"Duplicate action ID '{action.Id}'.", action.Id));

            foreach (var dependency in action.DependsOn)
            {
                if (!plan.Actions.Any(x => x.Id == dependency))
                    issues.Add(new("PLAN-003", ValidationSeverity.Error,
                        $"Dependency '{dependency}' does not exist.", action.Id));
            }

            foreach (var precondition in action.Preconditions)
            {
                if (precondition.Blocking)
                    issues.Add(new("PLAN-004", ValidationSeverity.Warning,
                        $"Blocking precondition must be satisfied before execution: {precondition.Description}",
                        action.Id));
            }

            if (action.Confidence < 0 || action.Confidence > 1)
                issues.Add(new("PLAN-005", ValidationSeverity.Error,
                    "Confidence must be between 0 and 1.", action.Id));
        }

        if (plan.Unknowns.Any(x => x.Blocking))
            issues.Add(new("PLAN-006", ValidationSeverity.Error,
                "Plan contains blocking unknown requirements."));

        if (plan.Actions.Any(x => x.RequiresApproval))
            issues.Add(new("PLAN-007", ValidationSeverity.Info,
                "One or more actions require explicit approval."));

        // Dependency order check.
        var positions = plan.Actions
            .Select((a, i) => (a.Id, Index: i))
            .ToDictionary(x => x.Id, x => x.Index, StringComparer.Ordinal);

        foreach (var action in plan.Actions)
        {
            foreach (var dep in action.DependsOn)
            {
                if (positions.TryGetValue(dep, out var depIndex) &&
                    positions[action.Id] <= depIndex)
                {
                    issues.Add(new("PLAN-008", ValidationSeverity.Error,
                        $"Action '{action.Id}' appears before dependency '{dep}'.",
                        action.Id));
                }
            }
        }

        // Conservative cycle check.
        if (HasCycle(plan.Actions))
            issues.Add(new("PLAN-009", ValidationSeverity.Critical,
                "Plan dependency graph contains a cycle."));

        var hasCritical = issues.Any(x => x.Severity == ValidationSeverity.Critical);
        var hasError = issues.Any(x => x.Severity == ValidationSeverity.Error);

        return new PlanValidationResult(
            IsValid: !hasCritical && !hasError,
            CanEnterApproval: !hasCritical && !hasError,
            Issues: issues);
    }

    private static bool HasCycle(IReadOnlyList<PlanAction> actions)
    {
        var map = actions.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        bool Visit(string id)
        {
            if (visiting.Contains(id)) return true;
            if (visited.Contains(id)) return false;

            visiting.Add(id);

            if (map.TryGetValue(id, out var action))
                foreach (var dep in action.DependsOn)
                    if (map.ContainsKey(dep) && Visit(dep))
                        return true;

            visiting.Remove(id);
            visited.Add(id);
            return false;
        }

        return actions.Any(x => Visit(x.Id));
    }
}
