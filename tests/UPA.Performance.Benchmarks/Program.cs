using System;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using UPA.Performance.Benchmarks.Scenarios;

namespace UPA.Performance.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = DefaultConfig.Instance.WithOption(ConfigOptions.DisableOptimizationsValidator, true);
            BenchmarkRunner.Run<P05_OptimizedDeepScan>(config, args);
        }
    }
}
