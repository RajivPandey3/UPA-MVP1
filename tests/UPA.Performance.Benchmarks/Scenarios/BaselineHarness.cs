using System;
using System.Diagnostics;
using System.Threading;
using UPA.Pipeline;
using UPA.Core;
using UPA.MVP3.TrustEmission;
using UPA.VerificationTrustAnchor;

namespace UPA.Performance.Benchmarks.Scenarios
{
    public class BaselineHarness
    {
        public static void RunP01AndP02()
        {
            Console.WriteLine("========================================");
            Console.WriteLine(" P01 (Baseline) vs P02 (UPA Initialized)");
            Console.WriteLine("========================================");

            // GC Warmup
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            long preMemory = GC.GetTotalMemory(true);

            // P01: Baseline
            Console.WriteLine("\n--- Running P01: Baseline (No UPA) ---");
            var sw1 = Stopwatch.StartNew();
            
            // Simulate Host Startup
            Thread.Sleep(50); // Simulate some host initialization
            
            // Simulate Representative Operation
            int sum = 0;
            for(int i=0; i<1000000; i++) sum += i;
            
            sw1.Stop();
            long p01Memory = GC.GetTotalMemory(false) - preMemory;

            Console.WriteLine($"[P01] Startup & Operation Time: {sw1.ElapsedMilliseconds} ms");
            Console.WriteLine($"[P01] Memory Growth: {p01Memory / 1024.0:F2} KB");

            // GC Reset
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            preMemory = GC.GetTotalMemory(true);

            // P02: UPA Initialized
            Console.WriteLine("\n--- Running P02: UPA Idle (With UPA) ---");
            var sw2 = Stopwatch.StartNew();

            // Simulate Host Startup
            Thread.Sleep(50);
            
            // Initialize UPA Components (Idle state, no execution)
            var pipeline = new GovernedPipeline();
            var trustEmitter = new TrustEmitter("dummy.json", new RegistryCertificateChain());

            // Simulate Representative Operation
            int sum2 = 0;
            for(int i=0; i<1000000; i++) sum2 += i;

            sw2.Stop();
            long p02Memory = GC.GetTotalMemory(false) - preMemory;

            Console.WriteLine($"[P02] Startup, UPA Init & Operation Time: {sw2.ElapsedMilliseconds} ms");
            Console.WriteLine($"[P02] Memory Growth: {p02Memory / 1024.0:F2} KB");

            // Deltas
            Console.WriteLine("\n--- Evidence Summary ---");
            Console.WriteLine($"Time Delta: {sw2.ElapsedMilliseconds - sw1.ElapsedMilliseconds} ms");
            Console.WriteLine($"Memory Delta: {(p02Memory - p01Memory) / 1024.0:F2} KB");
            Console.WriteLine("========================================\n");
        }
    }
}
