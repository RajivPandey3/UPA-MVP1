```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i5-8350U CPU 1.70GHz (Max: 1.90GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.424
  [Host]     : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3
  Job-IJPESX : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3

IterationCount=5  LaunchCount=1  WarmupCount=3  

```
| Method                   | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| P01_Baseline_Idle        | 400.5 ns | 138.57 ns | 21.44 ns |  1.00 |    0.07 |      - |         - |          NA |
| P02_UPA_Initialized_Idle | 490.4 ns |  47.60 ns | 12.36 ns |  1.23 |    0.06 | 0.0706 |     224 B |          NA |
