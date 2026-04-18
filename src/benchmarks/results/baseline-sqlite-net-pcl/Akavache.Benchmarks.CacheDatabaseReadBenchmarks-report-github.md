```

BenchmarkDotNet v0.15.8, Linux CachyOS
AMD Ryzen 7 5800X 1.75GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.104
  [Host]   : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3
  .NET 9.0 : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method         | BenchmarkSize | Mean        | Error     | StdDev     | Gen0    | Gen1    | Allocated  |
|--------------- |-------------- |------------:|----------:|-----------:|--------:|--------:|-----------:|
| **SequentialRead** | **10**            |   **432.97 μs** |  **6.144 μs** |   **5.447 μs** |  **5.8594** |       **-** |   **111.9 KB** |
| RandomRead     | 10            |   544.24 μs |  5.526 μs |   4.899 μs |  6.8359 |  0.9766 |  113.14 KB |
| BulkRead       | 10            |    78.80 μs |  1.537 μs |   1.708 μs |  0.9766 |       - |   18.59 KB |
| **SequentialRead** | **100**           | **4,129.32 μs** | **78.882 μs** | **102.569 μs** | **62.5000** |       **-** | **1130.78 KB** |
| RandomRead     | 100           | 5,117.07 μs | 61.720 μs |  51.539 μs | 62.5000 | 15.6250 | 1128.23 KB |
| BulkRead       | 100           |   277.72 μs |  5.294 μs |   5.664 μs |  3.9063 |       - |   78.06 KB |
| **SequentialRead** | **1000**          |          **NA** |        **NA** |         **NA** |      **NA** |      **NA** |         **NA** |
| RandomRead     | 1000          |          NA |        NA |         NA |      NA |      NA |         NA |
| BulkRead       | 1000          | 3,816.30 μs | 75.984 μs |  93.316 μs | 39.0625 | 15.6250 |  649.98 KB |

Benchmarks with issues:
  CacheDatabaseReadBenchmarks.SequentialRead: .NET 9.0(Runtime=.NET 9.0) [BenchmarkSize=1000]
  CacheDatabaseReadBenchmarks.RandomRead: .NET 9.0(Runtime=.NET 9.0) [BenchmarkSize=1000]
