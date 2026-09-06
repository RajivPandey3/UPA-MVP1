using UPA.Core;
using UPA.Execution;
using UPA.Planning;

namespace UPA.Pipeline.Tests;

public sealed class OwnershipTests
{
    [Fact]
    public void FailedLaunchCannotDeleteAnotherWritersScene()
    {
        var root = Directory.CreateTempSubdirectory("upa-ownership-");
        try
        {
            Directory.CreateDirectory(Path.Combine(root.FullName, "Assets"));
            Directory.CreateDirectory(Path.Combine(root.FullName, "ProjectSettings"));
            File.WriteAllText(Path.Combine(root.FullName, "ProjectSettings/ProjectVersion.txt"), "m_EditorVersion: 6000.0.36f1");
            var invalidExecutable = Path.Combine(root.FullName, "invalid.exe");
            File.WriteAllText(invalidExecutable, "not an executable");
            var plan = new IntentPlanner().BuildPlan("Create a GameObject named Player with a Rigidbody in the scene.");
            var scan = new ScanResult(EntityId.New(), DateTimeOffset.UtcNow, Array.Empty<Diagnostic>()) { ProjectRoot = root.FullName };
            var transaction = new UnityBatchPlanBinder(invalidExecutable, "Assets/Other.unity").Bind(plan, scan);
            Assert.ThrowsAny<Exception>(() => transaction.Execute(new ApprovalToken(plan.PlanId, "test", DateTimeOffset.UtcNow, true)));
            var scenePath = Path.Combine(root.FullName, "Assets/Other.unity");
            File.WriteAllText(scenePath, "another writer's scene");
            File.WriteAllText(scenePath + ".meta", "another writer's meta");
            transaction.Rollback();
            Assert.True(File.Exists(scenePath));
            Assert.Equal("another writer's scene", File.ReadAllText(scenePath));
            Assert.Equal("another writer's meta", File.ReadAllText(scenePath + ".meta"));
        }
        finally { root.Delete(true); }
    }
}
