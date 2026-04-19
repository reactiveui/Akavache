```

BenchmarkDotNet v0.15.8, Linux CachyOS
AMD Ryzen 7 5800X 1.75GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.104
  [Host]   : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3
  .NET 9.0 : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3

Job=.NET 9.0  Runtime=.NET 9.0  InvocationCount=1  
UnrollFactor=1  

```
| Method                 | BenchmarkSize | Mean         | Error      | StdDev      | Median       | Allocated  |
|----------------------- |-------------- |-------------:|-----------:|------------:|-------------:|-----------:|
| **BulkOperations**         | **10**            |     **2.095 ms** |  **0.0547 ms** |   **0.1604 ms** |     **2.087 ms** |   **60.28 KB** |
| **BulkOperations**         | **100**           |     **3.650 ms** |  **0.0792 ms** |   **0.2310 ms** |     **3.672 ms** |  **222.77 KB** |
| **BulkOperations**         | **1000**          |    **15.511 ms** |  **1.0022 ms** |   **2.9549 ms** |    **15.362 ms** | **1828.41 KB** |
|                        |               |              |            |             |              |            |
| **InsertWithExpiration**   | **10**            |    **30.054 ms** |  **1.4614 ms** |   **4.3091 ms** |    **28.248 ms** |   **62.61 KB** |
| **InsertWithExpiration**   | **100**           |   **280.817 ms** |  **5.5425 ms** |   **5.1845 ms** |   **282.132 ms** |  **284.33 KB** |
| **InsertWithExpiration**   | **1000**          | **2,844.729 ms** | **51.6314 ms** |  **83.3751 ms** | **2,831.169 ms** | **2512.18 KB** |
|                        |               |              |            |             |              |            |
| **GetAndFetchLatest**      | **10**            |    **29.847 ms** |  **1.5422 ms** |   **4.5472 ms** |    **27.598 ms** |  **105.57 KB** |
| **GetAndFetchLatest**      | **100**           |   **274.258 ms** |  **5.0336 ms** |   **4.7084 ms** |   **274.034 ms** |  **702.63 KB** |
| **GetAndFetchLatest**      | **1000**          |   **277.672 ms** |  **5.5261 ms** |  **10.3793 ms** |   **280.387 ms** |  **702.63 KB** |
|                        |               |              |            |             |              |            |
| **GetOrFetchObject**       | **10**            |    **30.787 ms** |  **1.3040 ms** |   **3.8448 ms** |    **29.535 ms** |  **144.16 KB** |
| **GetOrFetchObject**       | **100**           |   **284.787 ms** |  **4.9538 ms** |   **4.6338 ms** |   **285.656 ms** | **1099.94 KB** |
| **GetOrFetchObject**       | **1000**          | **2,892.610 ms** | **31.4916 ms** |  **27.9165 ms** | **2,889.415 ms** | **10668.3 KB** |
|                        |               |              |            |             |              |            |
| **InMemoryOperations**     | **10**            |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
| **InMemoryOperations**     | **100**           |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
| **InMemoryOperations**     | **1000**          |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
|                        |               |              |            |             |              |            |
| **InvalidateObjects**      | **10**            |    **30.855 ms** |  **1.3465 ms** |   **3.9702 ms** |    **28.984 ms** |   **70.45 KB** |
| **InvalidateObjects**      | **100**           |   **283.226 ms** |  **5.4302 ms** |   **6.2535 ms** |   **282.201 ms** |  **385.54 KB** |
| **InvalidateObjects**      | **1000**          | **3,051.262 ms** | **60.9803 ms** | **170.9953 ms** | **3,051.095 ms** | **3278.48 KB** |
|                        |               |              |            |             |              |            |
| **LocalMachineOperations** | **10**            |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
| **LocalMachineOperations** | **100**           |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
| **LocalMachineOperations** | **1000**          |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
|                        |               |              |            |             |              |            |
| **MixedOperations**        | **10**            |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
| **MixedOperations**        | **100**           |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
| **MixedOperations**        | **1000**          |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
|                        |               |              |            |             |              |            |
| **SecureOperations**       | **10**            |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
| **SecureOperations**       | **100**           |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
| **SecureOperations**       | **1000**          |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
|                        |               |              |            |             |              |            |
| **SerializerPerformance**  | **10**            |    **29.841 ms** |  **1.3644 ms** |   **4.0228 ms** |    **28.191 ms** |   **80.27 KB** |
| **SerializerPerformance**  | **100**           |   **279.025 ms** |  **5.5354 ms** |  **11.3073 ms** |   **279.318 ms** |  **452.61 KB** |
| **SerializerPerformance**  | **1000**          | **2,831.769 ms** | **29.1381 ms** |  **22.7491 ms** | **2,833.009 ms** | **4183.52 KB** |
|                        |               |              |            |             |              |            |
| **UserAccountOperations**  | **10**            |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
| **UserAccountOperations**  | **100**           |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |
| **UserAccountOperations**  | **1000**          |           **NA** |         **NA** |          **NA** |           **NA** |         **NA** |

Benchmarks with issues:
  CacheDatabaseComprehensiveBenchmarks.InMemoryOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=10]
  CacheDatabaseComprehensiveBenchmarks.InMemoryOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=100]
  CacheDatabaseComprehensiveBenchmarks.InMemoryOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=1000]
  CacheDatabaseComprehensiveBenchmarks.LocalMachineOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=10]
  CacheDatabaseComprehensiveBenchmarks.LocalMachineOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=100]
  CacheDatabaseComprehensiveBenchmarks.LocalMachineOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=1000]
  CacheDatabaseComprehensiveBenchmarks.MixedOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=10]
  CacheDatabaseComprehensiveBenchmarks.MixedOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=100]
  CacheDatabaseComprehensiveBenchmarks.MixedOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=1000]
  CacheDatabaseComprehensiveBenchmarks.SecureOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=10]
  CacheDatabaseComprehensiveBenchmarks.SecureOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=100]
  CacheDatabaseComprehensiveBenchmarks.SecureOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=1000]
  CacheDatabaseComprehensiveBenchmarks.UserAccountOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=10]
  CacheDatabaseComprehensiveBenchmarks.UserAccountOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=100]
  CacheDatabaseComprehensiveBenchmarks.UserAccountOperations: .NET 9.0(Runtime=.NET 9.0, InvocationCount=1, UnrollFactor=1) [BenchmarkSize=1000]
