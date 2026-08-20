using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using UPA.Analysis;
using UPA.Core;

namespace UPA.Performance.Benchmarks.Scenarios
{
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
    public class CSharpScannerProfiling
    {
        private string _tempDir = string.Empty;
        private CSharpScanner _scanner = new CSharpScanner();
        private ScanContext _context = default!;

        [GlobalSetup]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "UPA_ScannerProfile_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);
            Directory.CreateDirectory(Path.Combine(_tempDir, "Assets"));
            
            // Create 100 files, each with 10 classes and lots of text to simulate a real project
            for (int i = 0; i < 100; i++)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("using UnityEngine;");
                sb.AppendLine("namespace MyNamespace {");
                
                for (int j=0; j<10; j++)
                {
                    sb.AppendLine($"  [RequireComponent(typeof(BoxCollider))]");
                    sb.AppendLine($"  [SelectionBase]");
                    sb.AppendLine($"  public class MyBehaviour_{j} : MonoBehaviour {{");
                    sb.AppendLine($"      [SerializeField] private int _speed = 10;");
                    sb.AppendLine($"      public string Name;");
                    sb.AppendLine($"      private void Awake() {{ }}");
                    sb.AppendLine($"      private void Update() {{ }}");
                    sb.AppendLine($"      // Extra padding");
                    for (int p=0; p<50; p++) sb.AppendLine($"      // padding {p}");
                    sb.AppendLine($"  }}");
                }
                sb.AppendLine("}");
                
                File.WriteAllText(Path.Combine(_tempDir, "Assets", $"File_{i}.cs"), sb.ToString());
            }

            _context = new ScanContext(_tempDir, ReadOnly: true);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        }

        [Benchmark]
        public object ProfileCurrentScanner()
        {
            return _scanner.ScanAsync(_context).GetAwaiter().GetResult();
        }
    }
}
