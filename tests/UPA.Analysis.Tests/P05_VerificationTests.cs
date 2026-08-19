using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using UPA.Analysis;
using UPA.Core;
using System.Diagnostics;
using System.Linq;

namespace UPA.Analysis.Tests
{
    public class P05_VerificationTests
    {
        private string GenerateDummyProject(int fileCount)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "UPA_P05_Tests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var assets = Path.Combine(tempDir, "Assets");
            Directory.CreateDirectory(assets);
            Directory.CreateDirectory(Path.Combine(tempDir, "ProjectSettings"));
            File.WriteAllText(Path.Combine(tempDir, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.0.0f1\n");
            
            for (int i = 0; i < fileCount; i++)
            {
                File.WriteAllText(
                    Path.Combine(assets, $"DummyScript_{i}.cs"), 
                    $"namespace DummyNamespace {{ public class DummyScript_{i} : UnityEngine.MonoBehaviour {{ [UnityEngine.SerializeField] private int val{i}; private void Awake() {{ }} }} }}"
                );
            }
            return tempDir;
        }

        [Fact]
        public async Task P05_A_CallerThreadIsNonBlocking()
        {
            var dir = GenerateDummyProject(10000);
            try
            {
                var scanner = new ProjectScanner();
                var context = new ScanContext(dir);
                
                var sw = Stopwatch.StartNew();
                // This call should return immediately (e.g. < 50ms) without waiting for the 600ms scan
                var task = scanner.ScanAsync(context);
                sw.Stop();
                
                Assert.True(sw.ElapsedMilliseconds < 150, $"ScanAsync blocked the caller thread for {sw.ElapsedMilliseconds} ms!");
                
                var result = await task;
                Assert.NotNull(result);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public async Task P05_B_CorrectnessEquivalence()
        {
            var dir = GenerateDummyProject(100);
            try
            {
                var scanner = new ProjectScanner();
                var context = new ScanContext(dir);
                
                var syncResult = scanner.Scan(context);
                var asyncResult = await scanner.ScanAsync(context);
                
                Assert.Equal(syncResult.Assemblies.Count, asyncResult.Assemblies.Count);
                Assert.Equal(syncResult.Packages.Count, asyncResult.Packages.Count);
                Assert.Equal(syncResult.ProjectSettingsFiles.Count, asyncResult.ProjectSettingsFiles.Count);
                Assert.Equal(syncResult.Diagnostics.Count, asyncResult.Diagnostics.Count);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public async Task P05_C_CancellationPromptness()
        {
            var dir = GenerateDummyProject(20000); 
            try
            {
                var scanner = new CSharpScanner();
                var context = new ScanContext(dir);
                using var cts = new CancellationTokenSource();
                
                var sw = Stopwatch.StartNew();
                // We wrap it in Task.Run just for testing the token propagation
                var task = Task.Run(() => scanner.Scan(context, cts.Token), cts.Token);
                
                // Cancel immediately to ensure we catch it in the middle of the loop
                cts.Cancel();
                
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
                sw.Stop();
                
                Assert.True(sw.ElapsedMilliseconds < 500, $"Cancellation took too long: {sw.ElapsedMilliseconds} ms");
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public async Task P05_C_ProjectScanner_CancellationPromptness()
        {
            var dir = GenerateDummyProject(10000); 
            try
            {
                var scanner = new ProjectScanner();
                var context = new ScanContext(dir);
                using var cts = new CancellationTokenSource();
                cts.Cancel(); // Cancel before it even starts
                
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await scanner.ScanAsync(context, cts.Token));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public async Task P05_D_ConcurrentIsolation()
        {
            var dir1 = GenerateDummyProject(2000);
            var dir2 = GenerateDummyProject(2000);
            
            try
            {
                var scanner1 = new ProjectScanner();
                var scanner2 = new ProjectScanner();
                
                var task1 = scanner1.ScanAsync(new ScanContext(dir1));
                var task2 = scanner2.ScanAsync(new ScanContext(dir2));
                
                var results = await Task.WhenAll(task1, task2);
                
                Assert.Equal(2, results.Length);
                Assert.NotEqual(results[0].ProjectId, results[1].ProjectId);
                Assert.Equal(dir1, results[0].ProjectRoot);
                Assert.Equal(dir2, results[1].ProjectRoot);
            }
            finally
            {
                Directory.Delete(dir1, true);
                Directory.Delete(dir2, true);
            }
        }
    }
}
