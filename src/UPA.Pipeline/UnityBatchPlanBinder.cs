using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using UPA.Core;
using UPA.Execution;
using UPA.Governance;
using UPA.Planning;

namespace UPA.Pipeline;

public sealed class UnityBatchPlanBinder(string unityExecutable, string scenePath) : IPlanBinder
{
    public IVerifiedTransaction Bind(UpaPlan plan, ScanResult scan)
    {
        var specification = plan.UnityCreation;
        if (specification == null || !new PlanValidator().Validate(plan).IsValid ||
            specification.ComponentName != "Rigidbody" ||
            !Regex.IsMatch(specification.ObjectName, @"\A[A-Za-z][A-Za-z0-9_]{0,63}\z") ||
            plan.Actions.Count != 4 ||
            !plan.Actions.Any(action => action.Id == "create-gameobject" && action.Kind == PlanActionKind.Create) ||
            !plan.Actions.Any(action => action.Id == "configure-components" && action.Kind == PlanActionKind.Configure) ||
            !plan.Actions.Any(action => action.Id == "inspect-scene" && action.Kind == PlanActionKind.Inspect) ||
            !plan.Actions.Any(action => action.Id == "inspect-components" && action.Kind == PlanActionKind.Inspect))
            throw new InvalidOperationException("Supported command: Create a GameObject named Player with a Rigidbody in the scene.");
        if (plan.Actions.Any(action => action.Kind is PlanActionKind.Create or PlanActionKind.Configure && action.Target != specification.ObjectName) ||
            !plan.Actions.Single(action => action.Id == "configure-components").DependsOn.Contains("create-gameobject"))
            throw new InvalidOperationException("Typed operations do not match the validated creation specification.");
        if (!File.Exists(unityExecutable))
            throw new FileNotFoundException("Unity Editor executable was not found.", unityExecutable);
        if (!Regex.IsMatch(scenePath, @"\AAssets/(?:[A-Za-z0-9_-]+/)*[A-Za-z0-9_-]+\.unity\z"))
            throw new InvalidOperationException("The new scene must be an Assets-relative .unity path.");
        return new Transaction(unityExecutable, scan.ProjectRoot, scenePath, specification.ObjectName, plan.PlanId);
    }

    private sealed class Transaction(string unityExecutable, string root, string scenePath,
        string objectName, string planId) : IVerifiedTransaction
    {
        private string? _evidenceDirectory;
        private string? _stagingScenePath;
        private bool _published;
        public string Preview => $"Create new scene {scenePath}, GameObject {objectName}, and Rigidbody; save and reopen to verify.";
        public IReadOnlyList<OutputExpectation> ExpectedOutputs => new[] { new OutputExpectation(scenePath, "unity-player", objectName) };
        private string SceneFile => Path.Combine(root, scenePath);

        public void CheckPreconditions()
        {
            if (File.Exists(SceneFile) || File.Exists(SceneFile + ".meta"))
                throw new InvalidOperationException("This workflow only creates a new scene; the target already exists.");
            if (!File.Exists(Path.Combine(root, "ProjectSettings", "ProjectVersion.txt")))
                throw new InvalidOperationException("A Unity project with ProjectVersion.txt is required.");
        }

        public void Execute(ApprovalToken approval)
        {
            if (!approval.ExplicitlyApproved || approval.PlanId != planId)
                throw new InvalidOperationException("Approval does not match the plan.");
            CheckPreconditions();
            _evidenceDirectory = Path.Combine(root, "Library", "UPA", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_evidenceDirectory);
            _stagingScenePath = "Assets/UPAStaging-" + Guid.NewGuid().ToString("N") + "/Scene.unity";
            RunEditor("execute", approval.ApprovedBy, _stagingScenePath);
            OutputVerification.Verify(root, new[] { new OutputExpectation(_stagingScenePath, "unity-player", objectName) });
            Directory.CreateDirectory(Path.GetDirectoryName(SceneFile)!);
            File.Move(Path.Combine(root, _stagingScenePath), SceneFile, overwrite: false);
            _published = true;
            var stagingMeta = Path.Combine(root, _stagingScenePath) + ".meta";
            if (File.Exists(stagingMeta)) File.Move(stagingMeta, SceneFile + ".meta", overwrite: false);
        }

        public IReadOnlyList<string> VerifyOutput()
        {
            RunEditor("verify", "");
            if (!File.Exists(SceneFile)) throw new InvalidOperationException("The saved scene is missing.");
            CleanupStaging();
            return new[] { $"A fresh Unity process reopened {scenePath} and verified {objectName} with Rigidbody.",
                $"Verification evidence: {Path.Combine(_evidenceDirectory!, "verify-result.json")}" };
        }

        public void Rollback()
        {
            CleanupStaging();
            if (_published)
                throw new InvalidOperationException("Published scene retained for recovery review; rollback will not delete potentially modified output.");
        }

        private void CleanupStaging()
        {
            if (_stagingScenePath != null)
            {
                var stagingFile = Path.Combine(root, _stagingScenePath);
                if (File.Exists(stagingFile)) File.Delete(stagingFile);
                if (File.Exists(stagingFile + ".meta")) File.Delete(stagingFile + ".meta");
                var directory = Path.GetDirectoryName(stagingFile)!;
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: false);
                if (File.Exists(directory + ".meta")) File.Delete(directory + ".meta");
            }
        }

        private void RunEditor(string mode, string approvedBy, string? stagingScene = null)
        {
            if (_evidenceDirectory == null) throw new InvalidOperationException("Execution has not started.");
            var requestPath = Path.Combine(_evidenceDirectory, mode + "-request.json");
            var resultPath = Path.Combine(_evidenceDirectory, mode + "-result.json");
            File.WriteAllText(requestPath, JsonSerializer.Serialize(new {
                mode, scenePath = stagingScene ?? scenePath, objectName, planId, approvedBy, resultPath
            }));
            var start = new ProcessStartInfo(unityExecutable) { UseShellExecute = false, CreateNoWindow = true };
            foreach (var argument in new[] { "-batchmode", "-nographics", "-projectPath", root,
                "-executeMethod", "UPA.UnityExecutor.Editor.UpaUnityBatchBridge.Run", "-upaRequest", requestPath,
                "-logFile", Path.Combine(_evidenceDirectory, mode + ".log") })
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start Unity.");
            if (!process.WaitForExit(180000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
                throw new TimeoutException("Unity exceeded the three-minute execution limit.");
            }
            if (process.ExitCode != 0 || !File.Exists(resultPath))
                throw new InvalidOperationException($"Unity {mode} failed with exit code {process.ExitCode}. See {_evidenceDirectory}.");
            using var result = JsonDocument.Parse(File.ReadAllText(resultPath));
            if (!result.RootElement.GetProperty("verified").GetBoolean() ||
                result.RootElement.GetProperty("planId").GetString() != planId)
                throw new InvalidOperationException("Unity did not verify this plan's output.");
        }
    }
}
