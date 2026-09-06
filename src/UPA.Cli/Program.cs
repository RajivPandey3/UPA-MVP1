using System.Text.Json;
using UPA.Pipeline;

if (args.Length == 2 && args[0] == "inspect-runs")
{
    Console.WriteLine(JsonSerializer.Serialize(RunJournal.Inspect(args[1]), new JsonSerializerOptions { WriteIndented = true }));
    return RunJournal.Inspect(args[1]).Any(RunJournal.RequiresReview) ? 1 : 0;
}
if (args.Length != 4)
{
    Console.Error.WriteLine("Usage: UPA.Cli <Unity.exe> <project folder> <Assets/NewScene.unity> <request>");
    Console.Error.WriteLine("Or: UPA.Cli inspect-runs <project folder>");
    return 2;
}
var result = new GovernedPipeline().Execute(Guid.NewGuid().ToString("N"), args[1], args[3],
    new UnityBatchPlanBinder(args[0], args[2]), preview => ApprovalPrompt.Read(preview, Console.In, Console.Out));
Console.WriteLine(result.Success ? "Completed and verified." : "Not completed: " + string.Join("; ", result.Findings));
return result.Success ? 0 : 1;
