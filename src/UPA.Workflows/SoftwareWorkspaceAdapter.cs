using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UPA.Execution;

namespace UPA.Pipeline;

public sealed class SoftwareWorkspaceAdapter : IPlatformAdapter
{
    private static readonly Regex CreateText = new(
        @"^Create(?: a)? text file ""(?<path>[^""]+)"" with content ""(?<content>[\s\S]*)""\.?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Id => "software.workspace";
    public string Version => "1.0";
    public IReadOnlyList<string> Capabilities => new[] { "file.create.text" };

    public PreparedWorkflow Prepare(string projectRoot, string intent)
    {
        projectRoot = WorkspacePath.Root(projectRoot);
        var match = CreateText.Match(intent.Trim());
        if (!match.Success)
            throw new InvalidOperationException("Unsupported workspace intent. Supported form: Create text file \"name.txt\" with content \"...\".");

        var relativePath = match.Groups["path"].Value.Trim();
        var content = match.Groups["content"].Value;
        ValidateRelativePath(relativePath);
        if (content.Contains('\0')) throw new InvalidOperationException("File content cannot contain a null character.");

        var plan = new WorkflowPlan(
            Guid.NewGuid().ToString("N"), Id, Version, "file.create.text", intent);
        return new PreparedWorkflow(plan,
            new CreateTextFileTransaction(projectRoot, relativePath, content, plan.PlanId));
    }

    private static void ValidateRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidOperationException("Only a non-empty relative path is allowed.");
        var normalized = relativePath.Replace('\\', '/');
        if (normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(part => part == ".."))
            throw new InvalidOperationException("Workspace path traversal is not allowed.");
        if (normalized.StartsWith("/", StringComparison.Ordinal) || normalized.Contains(':'))
            throw new InvalidOperationException("Only a workspace-relative path is allowed.");
    }

    private sealed class CreateTextFileTransaction : IVerifiedTransaction
    {
        private readonly string root;
        private readonly string relativePath;
        private readonly string content;
        private readonly string planId;
        private string? createdPath;

        public CreateTextFileTransaction(string root, string relativePath, string content, string planId)
        {
            this.root = root;
            this.relativePath = relativePath;
            this.content = content;
            this.planId = planId;
            ExpectedOutputs = new[] { new OutputExpectation(relativePath, "text", content) };
        }

        public string Preview => $"Create text file '{relativePath}' ({Encoding.UTF8.GetByteCount(content)} bytes, SHA256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)))})";
        public IReadOnlyList<OutputExpectation> ExpectedOutputs { get; }

        public void CheckPreconditions()
        {
            var path = FullPath();
            if (File.Exists(path)) throw new IOException("Target file already exists; create-only operation refused.");
            if (Directory.Exists(path)) throw new IOException("Target path is an existing directory.");
        }

        public void Execute(ApprovalToken approval)
        {
            if (approval.PlanId != planId) throw new InvalidOperationException("Approval does not match the prepared plan.");
            var path = FullPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.Write(content);
            writer.Flush();
            createdPath = path;
        }

        public IReadOnlyList<string> VerifyOutput() =>
            new[] { $"Prepared output exists: {relativePath}" };

        public void Rollback()
        {
            if (createdPath is null || !File.Exists(createdPath)) return;
            var bytes = File.ReadAllBytes(createdPath);
            var expected = Encoding.UTF8.GetBytes(content);
            if (!bytes.SequenceEqual(expected))
                throw new IOException("Rollback refused because the created file changed after execution.");
            File.Delete(createdPath);
        }

        private string FullPath()
        {
            var path = Path.GetFullPath(Path.Combine(root, relativePath));
            var prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Workspace path escapes the project root.");
            return path;
        }
    }
}
