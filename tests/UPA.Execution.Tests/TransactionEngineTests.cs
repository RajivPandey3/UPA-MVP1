using UPA.Execution;

namespace UPA.Execution.Tests;

public class TransactionEngineTests
{
    [Fact]
    public void DryRunDoesNotModifyDisk()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var engine = new TransactionEngine(root.FullName);
            var result = engine.Execute(
                "plan-1",
                null,
                new[]
                {
                    new MutationRequest(
                        "op-1",
                        MutationKind.CreateTextFile,
                        "Assets/test.txt",
                        "hello")
                },
                dryRun: true);

            Assert.True(result.Success);
            Assert.False(File.Exists(Path.Combine(root.FullName, "Assets/test.txt")));
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void NonDryRunRequiresExplicitApproval()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var engine = new TransactionEngine(root.FullName);
            var result = engine.Execute(
                "plan-1",
                null,
                new[]
                {
                    new MutationRequest(
                        "op-1",
                        MutationKind.CreateTextFile,
                        "test.txt",
                        "hello")
                },
                dryRun: false);

            Assert.False(result.Success);
            Assert.False(File.Exists(Path.Combine(root.FullName, "test.txt")));
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void ApprovedTransactionCanCreateAllowlistedFile()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var engine = new TransactionEngine(root.FullName);
            var approval = new ApprovalToken(
                "plan-1", "human", DateTimeOffset.UtcNow, true);

            var result = engine.Execute(
                "plan-1",
                approval,
                new[]
                {
                    new MutationRequest(
                        "op-1",
                        MutationKind.CreateTextFile,
                        "Assets/test.txt",
                        "hello")
                },
                dryRun: false);

            Assert.True(result.Success);
            Assert.Equal("hello",
                File.ReadAllText(Path.Combine(root.FullName, "Assets/test.txt")));
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void SandboxBlocksPathTraversal()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var engine = new TransactionEngine(root.FullName);
            var approval = new ApprovalToken(
                "plan-1", "human", DateTimeOffset.UtcNow, true);

            var result = engine.Execute(
                "plan-1",
                approval,
                new[]
                {
                    new MutationRequest(
                        "op-1",
                        MutationKind.CreateTextFile,
                        "../escape.txt",
                        "blocked")
                },
                false);

            Assert.False(result.Success);
            Assert.False(File.Exists(Path.Combine(root.Parent!.FullName, "escape.txt")));
        }
        finally { root.Delete(true); }
    }

    [Fact]
    public void FailedTransactionRollsBackEarlierMutation()
    {
        var root = Directory.CreateTempSubdirectory();
        try
        {
            var engine = new TransactionEngine(root.FullName);
            var approval = new ApprovalToken(
                "plan-1", "human", DateTimeOffset.UtcNow, true);

            var result = engine.Execute(
                "plan-1",
                approval,
                new[]
                {
                    new MutationRequest(
                        "op-1",
                        MutationKind.CreateTextFile,
                        "first.txt",
                        "first"),
                    new MutationRequest(
                        "op-2",
                        MutationKind.CreateTextFile,
                        "first.txt",
                        "duplicate")
                },
                false);

            Assert.False(result.Success);
            Assert.True(result.RolledBack);
            Assert.False(File.Exists(Path.Combine(root.FullName, "first.txt")));
        }
        finally { root.Delete(true); }
    }
}
