using UPA.Planning;

namespace UPA.Governance;

public sealed class PreviewEngine
{
    public ApprovalPacket BuildApprovalPacket(
        UpaPlan plan,
        PlanValidationResult validation)
    {
        var preview = plan.Actions
            .Select(action => new PreviewChange(
                action.Id,
                action.Target,
                DescribeOperation(action),
                action.Risk.ToString(),
                action.Confidence,
                action.RequiresApproval))
            .ToArray();

        var risks = plan.Actions
            .GroupBy(x => x.Risk)
            .ToDictionary(x => x.Key, x => x.Count());

        var averageConfidence = plan.Actions.Count == 0
            ? 0
            : plan.Actions.Average(x => x.Confidence);

        var overall = plan.Actions.Any(x => x.Risk == PlanRisk.Critical)
            ? "Critical"
            : plan.Actions.Any(x => x.Risk == PlanRisk.High)
                ? "High"
                : plan.Actions.Any(x => x.Risk == PlanRisk.Medium)
                    ? "Medium"
                    : "Low";

        return new ApprovalPacket(
            plan.PlanId,
            plan.Intent,
            validation,
            preview,
            new RiskSummary(
                overall,
                risks.GetValueOrDefault(PlanRisk.Low),
                risks.GetValueOrDefault(PlanRisk.Medium),
                risks.GetValueOrDefault(PlanRisk.High),
                risks.GetValueOrDefault(PlanRisk.Critical),
                averageConfidence),
            "MVP-1: READ/ANALYZE/PLAN/PREVIEW only. No autonomous mutation.",
            false);
    }

    private static string DescribeOperation(PlanAction action)
        => action.Kind switch
        {
            PlanActionKind.Inspect => "Inspect existing state",
            PlanActionKind.Create => "Create requested structure",
            PlanActionKind.Configure => "Configure requested settings/components",
            PlanActionKind.Link => "Create requested reference/link",
            PlanActionKind.GeneratePlaceholder => "Generate clearly marked placeholder",
            PlanActionKind.Validate => "Validate preconditions/state",
            PlanActionKind.Report => "Generate report",
            PlanActionKind.AwaitUserInput => "Await required user input",
            _ => "Unknown operation"
        };
}
