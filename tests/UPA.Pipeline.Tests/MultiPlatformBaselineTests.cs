using UPA.Core;
using UPA.Execution;
using UPA.Planning;

namespace UPA.Pipeline.Tests;

public sealed class MultiPlatformBaselineTests
{
    [Fact]
    public void FileRequestMustNotRequireUnityVocabulary()
    {
        var root = Directory.CreateTempSubdirectory("upa-neutral-");
        try
        {
            var binder = new TextBinder(root.FullName);
            var result = new GovernedPipeline().Execute("files-baseline", root.FullName,
                "Create text file \"notes.txt\" with content \"hello\"", binder,
                preview => new ApprovalToken(preview.PlanId, "test", DateTimeOffset.UtcNow, true) { ContentHash = preview.ContentHash });
            Assert.True(result.Success, string.Join("; ", result.Findings));
            Assert.Equal("hello", File.ReadAllText(Path.Combine(root.FullName, "notes.txt")));
        }
        finally { root.Delete(true); }
    }

    private sealed class TextBinder(string root) : IPlanBinder, IVerifiedTransaction
    {
        public string Preview => "Create notes.txt containing hello";
        public IReadOnlyList<OutputExpectation> ExpectedOutputs => new[] { new OutputExpectation("notes.txt", "text", "hello") };
        public IVerifiedTransaction Bind(UpaPlan plan, ScanResult scan) => this;
        public void CheckPreconditions() { }
        public void Execute(ApprovalToken approval) => File.WriteAllText(Path.Combine(root, "notes.txt"), "hello");
        public IReadOnlyList<string> VerifyOutput() => Array.Empty<string>();
        public void Rollback() { }
    }
}
