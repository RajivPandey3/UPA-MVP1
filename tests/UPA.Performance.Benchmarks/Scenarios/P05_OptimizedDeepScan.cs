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
            _tempDir = Path.Combine(Path.GetTempPath(), "UPA_P05_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
            var assets = Path.Combine(_tempDir, "Assets");
            Directory.CreateDirectory(assets);
            Directory.CreateDirectory(Path.Combine(_tempDir, "ProjectSettings"));
            File.WriteAllText(Path.Combine(_tempDir, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.0.0f1\n");
            
            // Generate exactly 10,000 .cs files and 500 .asmdef files
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
        public object FullPipelineScan()
        {
            var p = _projectScanner.Scan(_context);
            var a = _assemblyScanner.Scan(_context);
            var c = _csharpScanner.Scan(_context);
            return c;
        }
    }
}
