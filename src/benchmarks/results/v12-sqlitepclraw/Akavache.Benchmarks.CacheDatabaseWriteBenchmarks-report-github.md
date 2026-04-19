```

BenchmarkDotNet v0.15.8, Linux CachyOS
AMD Ryzen 7 5800X 1.75GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.104
  [Host]   : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3
  .NET 9.0 : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3

Job=.NET 9.0  Runtime=.NET 9.0  InvocationCount=1  
UnrollFactor=1  

```
| Method                | BenchmarkSize | Mean           | Error        | StdDev       | Median         | Allocated  |
|---------------------- |-------------- |---------------:|-------------:|-------------:|---------------:|-----------:|
| **SequentialWrite**       | **10**            |       **493.3 μs** |      **8.82 μs** |     **18.80 μs** |       **494.8 μs** |   **16.55 KB** |
| SequentialObjectWrite | 10            |    29,049.7 μs |  1,318.24 μs |  3,886.87 μs |    27,346.7 μs |   63.53 KB |
| BulkWrite             | 10            |       101.2 μs |      2.01 μs |      5.22 μs |       100.2 μs |    10.8 KB |
| WriteWithExpiration   | 10            |       496.5 μs |     20.54 μs |     58.60 μs |       466.0 μs |   17.04 KB |
| **SequentialWrite**       | **100**           |     **6,940.7 μs** |    **939.75 μs** |  **2,770.87 μs** |     **5,270.9 μs** |  **172.05 KB** |
| SequentialObjectWrite | 100           |   277,979.9 μs |  5,359.51 μs |  6,581.96 μs |   277,580.1 μs |  292.98 KB |
| BulkWrite             | 100           |       476.8 μs |     10.73 μs |     30.95 μs |       471.8 μs |   85.69 KB |
| WriteWithExpiration   | 100           |     7,117.1 μs |    918.58 μs |  2,708.44 μs |     5,814.1 μs |  174.45 KB |
| **SequentialWrite**       | **1000**          |    **74,544.2 μs** |  **1,470.71 μs** |  **3,408.59 μs** |    **74,547.2 μs** | **1717.44 KB** |
| SequentialObjectWrite | 1000          | 2,835,216.0 μs | 55,138.49 μs | 46,043.14 μs | 2,830,355.3 μs | 2599.52 KB |
| BulkWrite             | 1000          |     4,877.7 μs |    238.56 μs |    657.05 μs |     4,889.7 μs |   824.7 KB |
| WriteWithExpiration   | 1000          |    89,116.4 μs |  1,766.37 μs |  3,914.15 μs |    89,480.5 μs | 1718.05 KB |
