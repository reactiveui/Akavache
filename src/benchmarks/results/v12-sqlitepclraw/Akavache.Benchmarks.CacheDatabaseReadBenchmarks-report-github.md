```

BenchmarkDotNet v0.15.8, Linux CachyOS
AMD Ryzen 7 5800X 1.75GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.104
  [Host]   : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3
  .NET 9.0 : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method         | BenchmarkSize | Mean        | Error     | StdDev    | Gen0     | Gen1     | Gen2     | Allocated  |
|--------------- |-------------- |------------:|----------:|----------:|---------:|---------:|---------:|-----------:|
| **SequentialRead** | **10**            |    **42.78 μs** |  **0.117 μs** |  **0.110 μs** |   **0.6714** |        **-** |        **-** |   **11.72 KB** |
| RandomRead     | 10            |          NA |        NA |        NA |       NA |       NA |       NA |         NA |
| BulkRead       | 10            |    24.96 μs |  0.100 μs |  0.089 μs |   0.9155 |        - |        - |   15.06 KB |
| **SequentialRead** | **100**           |   **393.24 μs** |  **3.281 μs** |  **3.069 μs** |   **6.8359** |        **-** |        **-** |   **113.8 KB** |
| RandomRead     | 100           |   371.79 μs |  2.480 μs |  2.198 μs |   7.3242 |   0.9766 |        - |  126.43 KB |
| BulkRead       | 100           |   175.80 μs |  0.458 μs |  0.429 μs |   8.0566 |   1.4648 |        - |  132.31 KB |
| **SequentialRead** | **1000**          | **5,138.22 μs** | **28.190 μs** | **26.369 μs** |  **62.5000** |        **-** |        **-** | **1132.19 KB** |
| RandomRead     | 1000          |          NA |        NA |        NA |       NA |       NA |       NA |         NA |
| BulkRead       | 1000          | 3,009.50 μs | 34.381 μs | 32.160 μs | 121.0938 | 121.0938 | 121.0938 | 1174.11 KB |

Benchmarks with issues:
  CacheDatabaseReadBenchmarks.RandomRead: .NET 9.0(Runtime=.NET 9.0) [BenchmarkSize=10]
  CacheDatabaseReadBenchmarks.RandomRead: .NET 9.0(Runtime=.NET 9.0) [BenchmarkSize=1000]
