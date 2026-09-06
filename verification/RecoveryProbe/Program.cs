using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using UPA.Core;
using UPA.Execution;
using UPA.Pipeline;
using UPA.Planning;

if (args.Length == 3 && args[0] == "child")
{
    var binder = new CrashTransaction(args[1], args[2]);
    new GovernedPipeline().Execute("crash-run", args[1], "Create a script.", binder,
        preview => new ApprovalToken(preview.PlanId, "crash test", DateTimeOffset.UtcNow, true) { ContentHash = preview.ContentHash });
    return 2;
}
var evidenceRoot = Path.GetFullPath(Path.Combine("verification", "cto-evidence", "crashes-" + Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(evidenceRoot);
var observations = new List<object>();
foreach (var stage in new[] { "Executing", "AfterWrite", "Verifying" })
{
    var root = Path.Combine(evidenceRoot, stage);
    Directory.CreateDirectory(root);
    var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true };
    if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
    foreach (var argument in new[] { "child", root, stage }) start.ArgumentList.Add(argument);
    using var process = Process.Start(start)!;
    try
    {
        var timeout = Stopwatch.StartNew();
        while (!File.Exists(Path.Combine(root, "ready")) && !process.HasExited && timeout.Elapsed < TimeSpan.FromSeconds(20))
            await Task.Delay(100);
        if (!File.Exists(Path.Combine(root, "ready"))) throw new InvalidOperationException("Child did not reach crash point: " + stage);
        process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync();
        var records = RunJournal.Inspect(root);
        if (records.Count != 1 || !RunJournal.RequiresReview(records[0])) throw new InvalidOperationException("Interrupted run was not detected.");
        var outputExists = File.Exists(Path.Combine(root, "proof.txt"));
        if (outputExists != (stage != "Executing")) throw new InvalidOperationException("Crash fixture output does not match its stage.");
        observations.Add(new { stage, killedProcess = process.Id, recoveredStatus = records[0].Status, requiresReview = true, outputExists });
    }
    finally { if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); } }
}
var report = JsonSerializer.Serialize(observations, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(Path.Combine(evidenceRoot, "results.json"), report);
Console.WriteLine(report);
Console.WriteLine("Evidence: " + evidenceRoot);
return 0;

sealed class CrashTransaction(string root, string crashStage) : IPlanBinder, IVerifiedTransaction
{
    public string Preview => "Create proof.txt";
    public IReadOnlyList<OutputExpectation> ExpectedOutputs => new[] { new OutputExpectation("proof.txt", "text", "proof") };
    public IVerifiedTransaction Bind(UpaPlan plan, ScanResult scan) => this;
    public void CheckPreconditions() { }
    public void Execute(ApprovalToken approval)
    {
        PauseAt("Executing");
        File.WriteAllText(Path.Combine(root, "proof.txt"), "proof");
        PauseAt("AfterWrite");
    }
    public IReadOnlyList<string> VerifyOutput()
    {
        PauseAt("Verifying");
        return Array.Empty<string>();
    }
    public void Rollback() => File.Delete(Path.Combine(root, "proof.txt"));
    private void PauseAt(string stage)
    {
        if (stage != crashStage) return;
        File.WriteAllText(Path.Combine(root, "ready"), stage);
        Thread.Sleep(Timeout.Infinite);
    }
}
