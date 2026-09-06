using System.Text.Json;
using UPA.Analysis;
using UPA.Core;
using UPA.Execution;
using UPA.Pipeline;
using UPA.Planning;

var root = Path.GetFullPath(Path.Combine("verification", "outsider-results", "probe-" + Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(root);
var engine = new TransactionEngine(root);
var approval = new ApprovalToken("outsider", "Outsider verification", DateTimeOffset.UtcNow, true);
var request = new[] { new MutationRequest("create", MutationKind.CreateTextFile, "Assets/proof.txt", "Created by UPA, verified by reading disk.") };
var dry = engine.Execute("outsider", null, request, true);
var dryUnchanged = !File.Exists(Path.Combine(root, "Assets/proof.txt"));
var denied = engine.Execute("outsider", null, request, false);
var deniedUnchanged = !File.Exists(Path.Combine(root, "Assets/proof.txt"));
var created = engine.Execute("outsider", approval, request, false);
var actualContent = File.ReadAllText(Path.Combine(root, "Assets/proof.txt"));
var rollback = engine.Execute("outsider", approval, new[] {
    new MutationRequest("first", MutationKind.CreateTextFile, "Assets/rollback.txt", "temporary"),
    new MutationRequest("duplicate", MutationKind.CreateTextFile, "Assets/rollback.txt", "collision")
}, false);
Directory.CreateDirectory(Path.Combine(root, "ProjectSettings"));
File.WriteAllText(Path.Combine(root, "ProjectSettings/ProjectVersion.txt"), "m_EditorVersion: 6000.0.36f1\n");
File.WriteAllText(Path.Combine(root, "Assets/Main.unity"), "--- !u!1 &100\nGameObject:\n  m_Name: OutsiderObject\n");
File.WriteAllText(Path.Combine(root, "Assets/Player.prefab"), "");
var scan = new ProjectScanner().Scan(new ScanContext(root));
var intents = new[] { "Create a GameObject in the scene.", "Do not create a GameObject in the scene.", "Do the thing." };
var plans = intents.Select(intent => {
    var plan = new IntentPlanner().BuildPlan(intent);
    return new { intent, actions = plan.Actions.Select(action => action.Id).ToArray(), plan.Executable, blockingUnknown = plan.Unknowns.Any(unknown => unknown.Blocking) };
}).ToArray();
var pipeline = new GovernedPipeline().Start("outsider", "This is nonsense and has no implementation", true, true, true, true, true, true, true);
var report = new {
    root,
    dryRunPreventedWrite = dry.Success && dryUnchanged,
    missingApprovalPreventedWrite = !denied.Success && deniedUnchanged,
    approvedWriteVerified = created.Success && actualContent == request[0].Content,
    actualContent,
    rollbackVerified = !rollback.Success && rollback.RolledBack && !File.Exists(Path.Combine(root, "Assets/rollback.txt")),
    scan.UnityVersion,
    scan.AssetPaths,
    plans,
    pipelineReportedSuccessForNonsense = pipeline.Success,
    pipelineStage = pipeline.State.Stage.ToString()
};
var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(Path.Combine(root, "observations.json"), json);
Console.WriteLine(json);
