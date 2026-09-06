using System.Text.Json;
using System.Diagnostics;
using UPA.Execution;
using UPA.Pipeline;

if (args.Length != 2) throw new ArgumentException("Supply Unity.exe and isolated Unity project paths.");
var root = Path.GetFullPath(args[1]);
var runId = "proof-" + Guid.NewGuid().ToString("N");
var scenePath = "Assets/" + runId + ".unity";
var binder = new UnityBatchPlanBinder(args[0], scenePath);
var pipeline = new GovernedPipeline();
var denied = pipeline.Execute(runId + "-denied", root,
    "Create a GameObject named Player with a Rigidbody in the scene.", binder, _ => null);
if (denied.Success || denied.State.Stage != PipelineStage.AwaitApproval || File.Exists(Path.Combine(root, scenePath)))
    throw new InvalidOperationException("Rejected approval did not prevent scene creation.");
var negative = pipeline.Execute(runId + "-negative", root,
    "Do not create a GameObject named Player with a Rigidbody in the scene.", binder,
    _ => throw new InvalidOperationException("Negative intent reached approval."));
if (negative.Success || File.Exists(Path.Combine(root, scenePath)))
    throw new InvalidOperationException("Negative intent was not blocked.");
var elapsed = Stopwatch.StartNew();
var result = pipeline.Execute(runId, root,
    "Create a GameObject named Player with a Rigidbody in the scene.", binder,
    preview => {
        Console.WriteLine("AUTOMATED TEST INPUT: APPROVE");
        return ApprovalPrompt.Read(preview, new StringReader("APPROVE"), Console.Out);
    });
var report = JsonSerializer.Serialize(new { runId, scenePath, elapsedSeconds = elapsed.Elapsed.TotalSeconds,
    rejectedApprovalPreventedWrite = true, negativeIntentPreventedWrite = true, result },
    new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(Path.Combine(root, "pipeline-proof.json"), report);
Console.WriteLine(report);
return result.Success ? 0 : 1;
