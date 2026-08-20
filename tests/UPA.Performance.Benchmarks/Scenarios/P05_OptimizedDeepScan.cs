using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using UPA.Analysis;
using UPA.Core;

namespace UPA.Performance.Benchmarks.Scenarios
{
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5)] // Keep iterations reasonable for large IO
    public class P05_OptimizedDeepScan
    {
        private string _tempDir = string.Empty;
        private ScanContext _context = default!;
        private ProjectScanner _projectScanner = new();
        private AssemblyScanner _assemblyScanner = new();
        private CSharpScanner _csharpScanner = new();

        [GlobalSetup]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "UPA_P05_" + "UPA_P05_BenchmarkProject");
            Directory.CreateDirectory(_tempDir);
            var assets = Path.Combine(_tempDir, "Assets");
            Directory.CreateDirectory(assets);
            Directory.CreateDirectory(Path.Combine(_tempDir, "ProjectSettings"));
            File.WriteAllText(Path.Combine(_tempDir, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.0.0f1\n");
            
            int fileCount = 10000;
            int asmdefCount = 500;

            for (int i = 0; i < fileCount; i++)
            {
                File.WriteAllText(
                    Path.Combine(assets, $"DummyScript_{i}.cs"), 
                    $"namespace DummyNamespace {{ public class DummyScript_{i} : UnityEngine.MonoBehaviour {{ [UnityEngine.SerializeField] private int val{i}; private void Awake() {{ }} }} }}"
                );
            }
            
            for (int i = 0; i < asmdefCount; i++)
            {
                File.WriteAllText(
                    Path.Combine(assets, $"DummyAsm_{i}.asmdef"), 
                    $"{{ \"name\": \"DummyAsm_{i}\" }}"
                );
            }

            _context = new ScanContext(_tempDir, ReadOnly: true);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        }

        [Benchmark]
        public async System.Threading.Tasks.Task<object> P05_C_Cold()
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(_tempDir));
            var hash = BitConverter.ToUInt64(hashBytes, 0);
            var cacheFile = Path.Combine(Path.GetTempPath(), $"upa_bin_cache_{hash:x16}.bin");
            if (File.Exists(cacheFile)) File.Delete(cacheFile);
            
            var c = await _csharpScanner.ScanDeltaAsync(_context);
            return c;
        }

        [Benchmark]
        public async System.Threading.Tasks.Task<object> P05_H_Hot()
        {
            var c = await _csharpScanner.ScanDeltaAsync(_context);
            return c;
        }

        [Benchmark]
        public async System.Threading.Tasks.Task<object> P05_X_Changed()
        {
            for (int i = 0; i < 5; i++)
            {
                var path = Path.Combine(_tempDir, "Assets", $"DummyScript_{i}.cs");
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                File.AppendAllText(path, "\n// Modified\n");
            }
            
            var c = await _csharpScanner.ScanDeltaAsync(_context);
            return c;
        }

        [Benchmark]
        public async System.Threading.Tasks.Task<object> P05_V_Invalidated()
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(_tempDir));
            var hash = BitConverter.ToUInt64(hashBytes, 0);
            var cacheFile = Path.Combine(Path.GetTempPath(), $"upa_bin_cache_{hash:x16}.bin");
            
            if (File.Exists(cacheFile))
            {
                using var fs = new FileStream(cacheFile, FileMode.Open, FileAccess.Write);
                fs.Seek(8, SeekOrigin.Begin);
                using var writer = new BinaryWriter(fs);
                writer.Write(9999);
            }

            var c = await _csharpScanner.ScanDeltaAsync(_context);
            return c;
        }
    }
}
