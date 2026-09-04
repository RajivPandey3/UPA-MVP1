using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UPA.Analysis;
using UPA.Core;
using System.IO;

namespace UPA.Performance.Benchmarks.Scenarios
{
    public class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<Tuple<SendOrPostCallback, object?>> _queue = new();
        public override void Post(SendOrPostCallback d, object? state) => _queue.Add(Tuple.Create(d, state));
        public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();
        public bool TryPump(out TimeSpan maxHitch)
        {
            maxHitch = TimeSpan.Zero;
            var sw = new Stopwatch();
            bool pumped = false;
            while (_queue.TryTake(out var item))
            {
                pumped = true;
                sw.Restart();
                item.Item1(item.Item2);
                sw.Stop();
                if (sw.Elapsed > maxHitch) maxHitch = sw.Elapsed;
            }
            return pumped;
        }
    }

    public static class P05_UserImpactProfiler
    {
        public static void Run()
        {
            Console.WriteLine("--- P05 USER IMPACT PROFILER ---");
            string testDir = Path.Combine(Path.GetTempPath(), "UPA_UserImpact_" + Guid.NewGuid().ToString());
            string assetsDir = Path.Combine(testDir, "Assets");
            Directory.CreateDirectory(assetsDir);

            for (int i = 0; i < 10000; i++)
            {
                File.WriteAllText(Path.Combine(assetsDir, $"DummyScript_{i}.cs"), 
                $"namespace DummyNamespace {{ public class DummyScript_{i} : UnityEngine.MonoBehaviour {{ [UnityEngine.SerializeField] private int val{i}; private void Awake() {{ }} }} }}");
            }

            var scanner = new CSharpScanner();
            var context = new ScanContext(testDir, false);
            
            Console.WriteLine("\n[1] Measuring Cold Scan Hitch (P05-C)...");
            MeasureHitch(scanner, context);
            
            Console.WriteLine("\n[2] Measuring Hot Incremental Scan Hitch (P05-H)...");
            MeasureHitch(scanner, context);

            Console.WriteLine("\n[3] Measuring Single File Change Hitch (P05-X)...");
            File.WriteAllText(Path.Combine(assetsDir, $"DummyScript_0.cs"), 
                $"namespace DummyNamespace {{ public class DummyScript_0 : UnityEngine.MonoBehaviour {{ public int Changed; }} }}");
            MeasureHitch(scanner, context);

            Directory.Delete(testDir, true);
        }

        private static void MeasureHitch(CSharpScanner scanner, ScanContext context)
        {
            var syncCtx = new SingleThreadSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            var swTotal = Stopwatch.StartNew();
            TimeSpan maxHitch = TimeSpan.Zero;
            
            var task = scanner.ScanAsync(context, CancellationToken.None);
            
            while (!task.IsCompleted)
            {
                if (syncCtx.TryPump(out var hitch))
                {
                    if (hitch > maxHitch) maxHitch = hitch;
                }
                Thread.Sleep(1); // Simulating frame boundary
            }
            swTotal.Stop();

            Console.WriteLine($"  Total Elapsed Time: {swTotal.ElapsedMilliseconds} ms");
            Console.WriteLine($"  Max Main Thread Hitch (Blocked Time): {maxHitch.TotalMilliseconds:F2} ms");
            if (maxHitch.TotalMilliseconds < 16.0)
                Console.WriteLine($"  Frame Stutter: NONE (Max hitch is under 16ms budget for 60fps)");
            else
                Console.WriteLine($"  Frame Stutter: OBSERVED");
                
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }
}
