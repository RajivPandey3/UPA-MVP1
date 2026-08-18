using UPA.ProjectModel;

namespace UPA.Health;

public sealed class ArchitectureHealthAnalyzer
{
    public ArchitectureHealthReport Analyze(UnifiedProjectModel model)
    {
        var findings = new List<HealthFinding>();
        var signals = new List<string>();

        var counts = model.Counts;

        if (counts.UnresolvedReferences > 0)
        {
            findings.Add(new HealthFinding(
                "HEALTH-REF-001",
                FindingSeverity.Error,
                "Unresolved references detected",
                $"{counts.UnresolvedReferences} unresolved references out of {counts.References} total.",
                "Resolve missing dependencies before enabling autonomous project mutation."));
        }

        if (counts.Diagnostics > 0)
        {
            findings.Add(new HealthFinding(
                "HEALTH-DIAG-001",
                FindingSeverity.Warning,
                "Scanner diagnostics exist",
                $"{counts.Diagnostics} diagnostics were reported by the scanner layer.",
                "Review diagnostics and distinguish expected project conditions from defects."));
        }

        if (counts.Scripts > 0 && counts.Assemblies == 0)
        {
            findings.Add(new HealthFinding(
                "HEALTH-ASM-001",
                FindingSeverity.Warning,
                "Scripts exist without explicit assembly definitions",
                $"{counts.Scripts} scripts were discovered but no .asmdef assemblies were reported.",
                "Confirm whether the project intentionally relies on predefined Unity assemblies."));
        }

        if (counts.Scenes == 0)
        {
            findings.Add(new HealthFinding(
                "HEALTH-SCENE-001",
                FindingSeverity.Warning,
                "No scene assets discovered",
                "The unified model contains zero scenes.",
                "Verify that scene assets are present or explicitly mark the project as code-only."));
        }

        if (counts.Assets == 0 && counts.Scripts > 0)
        {
            findings.Add(new HealthFinding(
                "HEALTH-ASSET-001",
                FindingSeverity.Info,
                "Code-only project signal",
                "Scripts were found while no asset records were reported.",
                "Verify scanner coverage before assuming production assets are absent."));
        }

        if (counts.GameObjects == 0 && counts.Scenes > 0)
        {
            findings.Add(new HealthFinding(
                "HEALTH-SCENE-002",
                FindingSeverity.Warning,
                "Scenes contain no reported GameObjects",
                $"{counts.Scenes} scene(s) exist but the model reports zero GameObjects.",
                "Verify scene scanning coverage and scene content."));
        }

        if (!string.IsNullOrWhiteSpace(model.RenderPipeline))
            signals.Add($"Render pipeline: {model.RenderPipeline}");

        if (!string.IsNullOrWhiteSpace(model.UnityVersion))
            signals.Add($"Unity version: {model.UnityVersion}");

        if (counts.Assemblies > 0)
            signals.Add("Explicit assembly architecture detected.");

        if (counts.Prefabs > 0)
            signals.Add("Prefab-based composition detected.");

        if (counts.References > 0)
            signals.Add("Asset dependency graph is populated.");

        var score = CalculateScore(model, findings);
        return new ArchitectureHealthReport(
            score,
            findings
                .OrderByDescending(x => x.Severity)
                .ThenBy(x => x.Code, StringComparer.Ordinal)
                .ToArray(),
            signals
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray(),
            DateTimeOffset.UtcNow,
            "1.0");
    }

    private static HealthScore CalculateScore(
        UnifiedProjectModel model,
        IReadOnlyList<HealthFinding> findings)
    {
        var score = 100.0;

        foreach (var finding in findings)
        {
            score -= finding.Severity switch
            {
                FindingSeverity.Critical => 30,
                FindingSeverity.Error => 15,
                FindingSeverity.Warning => 6,
                FindingSeverity.Info => 1,
                _ => 0
            };
        }

        score = Math.Clamp(score, 0, 100);

        var grade = score switch
        {
            >= 95 => "A+",
            >= 90 => "A",
            >= 80 => "B",
            >= 70 => "C",
            >= 60 => "D",
            _ => "F"
        };

        var categories = new Dictionary<string, double>
        {
            ["References"] = model.Counts.References == 0
                ? 100
                : 100.0 * (model.Counts.References - model.Counts.UnresolvedReferences)
                    / model.Counts.References,
            ["Diagnostics"] = model.Counts.Diagnostics == 0 ? 100 : 70,
            ["Structure"] = model.Counts.Assemblies > 0 || model.Counts.Scripts == 0 ? 100 : 90
        };

        return new HealthScore(score, grade, categories);
    }
}
