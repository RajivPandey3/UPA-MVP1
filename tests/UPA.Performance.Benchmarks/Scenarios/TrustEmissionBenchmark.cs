using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using UPA.MVP3.TrustEmission;
using UPA.VerificationTrustAnchor;

namespace UPA.Performance.Benchmarks.Scenarios
{
    [MemoryDiagnoser]
    public class TrustEmissionBenchmark
    {
        private TrustEmitter _emitter = null!;
        private string _stateFile = null!;
        private RegistryCertificateChain _registry = null!;

        [GlobalSetup]
        public void Setup()
        {
            _stateFile = Path.Combine(Path.GetTempPath(), "bench_trust_state.json");
            if (File.Exists(_stateFile)) File.Delete(_stateFile);
            
            _registry = new RegistryCertificateChain();
            _emitter = new TrustEmitter(_stateFile, _registry);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (File.Exists(_stateFile)) File.Delete(_stateFile);
        }

        [Benchmark]
        public async Task Emit_NewBundle()
        {
            var req = new TrustEmissionRequest(
                Guid.NewGuid().ToString("N"),
                Guid.NewGuid().ToString("N"),
                "hash",
                "snapshot"
            );
            await _emitter.EmitAsync(req);
        }

        [Benchmark]
        public async Task Emit_Idempotent()
        {
            var req = new TrustEmissionRequest(
                "fixed-run-id",
                "fixed-bundle-id",
                "fixed-hash",
                "fixed-snapshot"
            );
            await _emitter.EmitAsync(req);
        }
    }
}
