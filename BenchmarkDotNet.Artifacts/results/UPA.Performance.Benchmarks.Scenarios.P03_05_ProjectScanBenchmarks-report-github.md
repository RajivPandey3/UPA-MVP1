```

BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6456/22H2/2022Update)
Intel Core i5-8350U CPU 1.70GHz (Max: 1.90GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 8.0.424
  [Host]     : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3
  Job-IJPESX : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3
  Job-OOTPKI : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3


```
| Method      | Job        | Toolchain | IterationCount | LaunchCount | WarmupCount | FileCount | Mean       | Error     | StdDev    | Gen0   | Allocated |
|------------ |----------- |---------- |--------------- |------------ |------------ |---------- |-----------:|----------:|----------:|-------:|----------:|
| **ScanProject** | **Job-IJPESX** | **Default**   | **5**              | **1**           | **3**           | **10**        |   **521.8 μs** |  **47.93 μs** |   **7.42 μs** | **5.8594** |  **20.28 KB** |
| ScanProject | Job-OOTPKI | .NET 8.0  | Default        | Default     | Default     | 10        |   524.6 μs |  20.00 μs |  58.96 μs | 5.8594 |  20.28 KB |
| **ScanProject** | **Job-IJPESX** | **Default**   | **5**              | **1**           | **3**           | **1000**      | **1,637.6 μs** | **836.66 μs** | **217.28 μs** | **5.8594** |  **20.29 KB** |
| ScanProject | Job-OOTPKI | .NET 8.0  | Default        | Default     | Default     | 1000      | 1,013.4 μs |  19.86 μs |  22.08 μs | 5.8594 |  20.29 KB |
| **ScanProject** | **Job-IJPESX** | **Default**   | **5**              | **1**           | **3**           | **10000**     | **7,033.1 μs** | **477.66 μs** | **124.05 μs** |      **-** |  **20.31 KB** |
| ScanProject | Job-OOTPKI | .NET 8.0  | Default        | Default     | Default     | 10000     | 7,043.2 μs | 138.56 μs | 122.83 μs |      - |  20.31 KB |
