```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i5-8350U CPU 1.70GHz (Max: 1.90GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.424
  [Host]     : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3
  Job-WRIKEU : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3
  Job-OOTPKI : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3


```
| Method           | Job        | Toolchain | IterationCount | LaunchCount | WarmupCount | Mean    | Error    | StdDev   | Gen0       | Gen1      | Gen2      | Allocated |
|----------------- |----------- |---------- |--------------- |------------ |------------ |--------:|---------:|---------:|-----------:|----------:|----------:|----------:|
| FullPipelineScan | Job-WRIKEU | Default   | 5              | 1           | 2           | 1.048 s | 0.0493 s | 0.0128 s | 28000.0000 | 7000.0000 | 1000.0000 | 157.82 MB |
| FullPipelineScan | Job-OOTPKI | .NET 8.0  | Default        | Default     | Default     | 1.054 s | 0.0166 s | 0.0148 s | 29000.0000 | 8000.0000 | 2000.0000 | 157.83 MB |
