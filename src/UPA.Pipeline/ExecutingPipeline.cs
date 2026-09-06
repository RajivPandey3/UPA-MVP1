using UPA.Analysis;
using UPA.Core;
using UPA.Execution;
using UPA.Governance;
using UPA.Health;
using UPA.Planning;
using System.Text.Json;

namespace UPA.Pipeline;

public interface IPlanBinder
{
    IVerifiedTransaction Bind(UpaPlan plan, ScanResult scan);
}

public sealed partial class GovernedPipeline
{
    public PipelineResult Execute(string runId, string projectRoot, string intent, IPlanBinder binder,
        Func<ExecutionPreview, ApprovalToken?> requestApproval)
        => new WorkflowRunner(OutputVerification.CreateRegistry()).Execute(runId, projectRoot, intent,
            new UnityWorkflowAdapter(binder), requestApproval);
}

public sealed class UnityWorkflowAdapter(IPlanBinder binder) : IPlatformAdapter
{
    public string Id => "unity";
    public string Version => "1";
    public IReadOnlyList<string> Capabilities => new[] { "scene.create-player" };
    public bool IsFallback => false;
    public bool Detect(string root) => File.Exists(Path.Combine(root, "ProjectSettings", "ProjectVersion.txt"));

    public PreparedWorkflow Prepare(string root, string intent)
    {
        var context = new ScanContext(root);
        var scan = new ProjectScanner().Scan(context);
        var scripts = new CSharpScanner().Scan(context);
        var assemblies = new AssemblyScanner().Scan(context);
        var model = new UnifiedProjectModelComposer().Compose(scan, scripts, assemblies);
        var health = new ArchitectureHealthAnalyzer().Analyze(model);
        if (health.Findings.Any(finding => finding.Severity >= FindingSeverity.Error))
            throw new InvalidOperationException("Project health has blocking findings.");
        var plan = new IntentPlanner().BuildPlan(intent, model, health);
        var validation = new PlanValidator().Validate(plan);
        if (!validation.IsValid)
            throw new InvalidOperationException(string.Join("; ", validation.Issues
                .Where(issue => issue.Severity >= ValidationSeverity.Error).Select(issue => issue.Message)));
        return new PreparedWorkflow(new WorkflowPlan(plan.PlanId, Id, Version, Capabilities[0],
            JsonSerializer.Serialize(plan)), binder.Bind(plan, scan));
    }
}
