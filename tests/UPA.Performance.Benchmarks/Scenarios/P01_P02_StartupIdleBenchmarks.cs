using BenchmarkDotNet.Attributes;
using UPA.Pipeline;
using UPA.MVP3.TrustEmission;
using UPA.VerificationTrustAnchor;

namespace UPA.Performance.Benchmarks.Scenarios
{
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
    public class P01_P02_StartupIdleBenchmarks
    {
        [Benchmark(Baseline = true)]
        public int P01_Baseline_Idle()
        {
            // Simulate representative workload
            int sum = 0;
            for(int i=0; i<1000; i++) sum += i;
            return sum;
        }

        [Benchmark]
        public int P02_UPA_Initialized_Idle()
        {
            // Initialize UPA Components (Idle state)
            var pipeline = new GovernedPipeline();
            var trustEmitter = new TrustEmitter("dummy.json", new RegistryCertificateChain());
            
            // Simulate representative workload
            int sum = 0;
            for(int i=0; i<1000; i++) sum += i;
            return sum;
        }
    }
}
