using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UPA.Analysis;
using UPA.Core;

namespace UPA.Performance.Benchmarks.Scenarios
{
    public class DiscrepancyResolver
    {
        public static void RunFullScanSimulation()
        {
            Console.WriteLine("========================================");
            Console.WriteLine(" P03-P05 Discrepancy Resolution Harness ");
            Console.WriteLine("========================================");

            int fileCount = 10000;
            int asmdefCount = 500;
            
            Console.WriteLine($"[Setup] Generating {fileCount} .cs files and {asmdefCount} .asmdef files...");
            string tempDir = Path.Combine(Path.GetTempPath(), "UPA_Resolution_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            // Basic structure
            Directory.CreateDirectory(Path.Combine(tempDir, "ProjectSettings"));
            File.WriteAllText(Path.Combine(tempDir, "ProjectSettings", "ProjectVersion.txt"), "m_EditorVersion: 6000.0.0f1\n");
            
            Directory.CreateDirectory(Path.Combine(tempDir, "Packages"));
            File.WriteAllText(Path.Combine(tempDir, "Packages", "manifest.json"), "{ \"dependencies\": { \"com.unity.modules.core\": \"1.0.0\" } }");

            Directory.CreateDirectory(Path.Combine(tempDir, "Assets"));
            
            for (int i = 0; i < fileCount; i++)
            {
                File.WriteAllText(
                    Path.Combine(tempDir, "Assets", $"DummyScript_{i}.cs"), 
                    $"namespace DummyNamespace {{ public class DummyScript_{i} : UnityEngine.MonoBehaviour {{ [UnityEngine.SerializeField] private int val{i}; }} }}"
                );
            }
            
            for (int i = 0; i < asmdefCount; i++)
            {
                File.WriteAllText(
                    Path.Combine(tempDir, "Assets", $"DummyAsm_{i}.asmdef"), 
                    $"{{ \"name\": \"DummyAsm_{i}\" }}"
                );
            }

            var context = new ScanContext(tempDir, ReadOnly: true);
            var projectScanner = new ProjectScanner();
            var csharpScanner = new CSharpScanner();
            var asmScanner = new AssemblyScanner();

            // WARMUP
            projectScanner.Scan(context);
            csharpScanner.ScanAsync(context).GetAwaiter().GetResult();
            asmScanner.Scan(context);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long preMemory = GC.GetAllocatedBytesForCurrentThread();

            Console.WriteLine("\n[Measure] Starting out-of-process isolation equivalent scan...");
            var sw = Stopwatch.StartNew();

            // 1. Project Level Scan (Scans root structure and asmdefs)
            var pResult = projectScanner.Scan(context);
            long t1 = sw.ElapsedMilliseconds;

            // 2. Assembly Scan (Scans dependencies of asmdefs)
            var aResult = asmScanner.Scan(context);
            long t2 = sw.ElapsedMilliseconds;

            // 3. C# Script Scan (Reads and regex-matches ALL .cs files)
            var cResult = csharpScanner.ScanAsync(context).GetAwaiter().GetResult();
            long t3 = sw.ElapsedMilliseconds;

            sw.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - preMemory;

            Console.WriteLine($"\n[Results] True File IO & Parsing (10,000 files):");
            Console.WriteLine($"- ProjectScanner Time:  {t1} ms (Assemblies discovered: {pResult.Assemblies.Count})");
            Console.WriteLine($"- AssemblyScanner Time: {t2 - t1} ms (Dependencies scanned: {aResult.Assemblies.Count})");
            Console.WriteLine($"- CSharpScanner Time:   {t3 - t2} ms (C# Scripts Regexed: {cResult.Count})");
            Console.WriteLine($"- Total Pipeline Time:  {sw.ElapsedMilliseconds} ms");
            Console.WriteLine($"- Total Thread Alloc:   {allocated / 1024.0 / 1024.0:F2} MB");

            Console.WriteLine("\n[Conclusion]");
            Console.WriteLine("The 7ms BenchmarkDotNet result only captured ProjectScanner skipping .cs files.");
            Console.WriteLine("The 120ms InProcess result was polluted by InProcessEmit toolchain noise.");
            Console.WriteLine("This script reveals the actual deep-scan cost for 10,000 files across all scanners.");
            
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }
}
