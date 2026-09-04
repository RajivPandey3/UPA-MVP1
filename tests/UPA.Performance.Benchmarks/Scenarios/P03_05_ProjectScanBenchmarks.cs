using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using UPA.Analysis;
using UPA.Core;

namespace UPA.Performance.Benchmarks.Scenarios
{
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
    public class P03_05_ProjectScanBenchmarks
    {
        private string _tempDir = string.Empty;
        private ProjectScanner _scanner = new ProjectScanner();
        private ScanContext _context = default!;

        [Params(10, 1000, 10000)]
        public int FileCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"UPA_Scan_{FileCount}_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            // Create Unity project structure
            Directory.CreateDirectory(Path.Combine(_tempDir, "ProjectSettings"));
            File.WriteAllText(
                Path.Combine(_tempDir, "ProjectSettings", "ProjectVersion.txt"),
                "m_EditorVersion: 6000.0.0f1\n");

            Directory.CreateDirectory(Path.Combine(_tempDir, "Packages"));
            File.WriteAllText(
                Path.Combine(_tempDir, "Packages", "manifest.json"),
                "{ \"dependencies\": { \"com.unity.modules.core\": \"1.0.0\" } }");

            Directory.CreateDirectory(Path.Combine(_tempDir, "Assets"));
            
            // Create files
            for (int i = 0; i < FileCount; i++)
            {
                File.WriteAllText(Path.Combine(_tempDir, "Assets", $"DummyScript_{i}.cs"), "public class Dummy {}");
            }

            _context = new ScanContext(_tempDir, ReadOnly: true);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Benchmark]
        public ScanResult ScanProject()
        {
            return _scanner.Scan(_context);
        }
    }
}
