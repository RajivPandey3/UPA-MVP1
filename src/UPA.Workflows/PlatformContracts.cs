using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UPA.Pipeline;

public sealed record OutputExpectation(string RelativePath, string Kind, string Value);

public sealed record WorkflowPlan(string PlanId, string PlatformId, string Version,
    string CapabilityId, string Intent);

public sealed record PreparedWorkflow(WorkflowPlan Plan, IVerifiedTransaction Transaction);

public interface IPlatformAdapter
{
    string Id { get; }
    string Version { get; }
    IReadOnlyList<string> Capabilities { get; }
    PreparedWorkflow Prepare(string projectRoot, string intent);
}

public static class WorkspacePath
{
    public static string Root(string root)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Workspace root is required.", nameof(root));
        var full = Path.GetFullPath(root);
        if (!Directory.Exists(full)) throw new DirectoryNotFoundException(full);
        return full;
    }
}

public static class WorkflowFingerprint
{
    public static string Compute(string root, WorkflowPlan plan, string preview, IReadOnlyList<OutputExpectation> outputs)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { root, plan, preview, outputs }))));
}

public sealed class OutputVerifierRegistry
{
    public IReadOnlyList<string> Verify(string root, IReadOnlyList<OutputExpectation> outputs)
    {
        if (outputs.Count == 0) throw new InvalidOperationException("No output expectations were supplied.");
        var evidence = new List<string>();
        foreach (var output in outputs)
        {
            var path = Path.GetFullPath(Path.Combine(root, output.RelativePath));
            if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Output escapes the workspace.");
            if (!File.Exists(path)) throw new FileNotFoundException("Expected output was not found.", path);
            var bytes = File.ReadAllBytes(path);
            if (output.Kind == "text" && Encoding.UTF8.GetString(bytes) != output.Value)
                throw new InvalidOperationException("Text output differs from expectation.");
            if (output.Kind != "text")
                throw new InvalidOperationException("No verifier is registered for output kind '" + output.Kind + "'.");
            evidence.Add($"Verified {output.RelativePath}; SHA256={Convert.ToHexString(SHA256.HashData(bytes))}");
        }
        return evidence;
    }
}
