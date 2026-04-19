```

BenchmarkDotNet v0.15.8, Linux CachyOS
AMD Ryzen 7 5800X 1.75GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.104
  [Host]   : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3
  .NET 9.0 : .NET 9.0.14 (9.0.14, 9.0.1426.11910), X64 RyuJIT x86-64-v3

Job=.NET 9.0  Runtime=.NET 9.0  InvocationCount=1  
UnrollFactor=1  

```
| Method                | BenchmarkSize | Mean         | Error      | StdDev      | Median       | Allocated  |
|---------------------- |-------------- |-------------:|-----------:|------------:|-------------:|-----------:|
| **SequentialWrite**       | **10**            |    **43.099 ms** |  **2.1105 ms** |   **6.1896 ms** |    **40.414 ms** |   **34.31 KB** |
| SequentialObjectWrite | 10            |    42.072 ms |  1.4755 ms |   4.2807 ms |    40.269 ms |   89.16 KB |
| BulkWrite             | 10            |     4.564 ms |  0.2318 ms |   0.6650 ms |     4.658 ms |   13.36 KB |
| WriteWithExpiration   | 10            |    41.372 ms |  1.5027 ms |   4.3357 ms |    39.746 ms |   35.46 KB |
| **SequentialWrite**       | **100**           |   **384.294 ms** |  **6.9566 ms** |   **7.7322 ms** |   **385.957 ms** |   **345.3 KB** |
| SequentialObjectWrite | 100           |   398.831 ms |  7.9471 ms |   9.4604 ms |   399.225 ms |  548.45 KB |
| BulkWrite             | 100           |     4.793 ms |  0.0956 ms |   0.2744 ms |     4.838 ms |   102.9 KB |
| WriteWithExpiration   | 100           |   382.713 ms |  7.5983 ms |  11.3728 ms |   384.871 ms |  336.71 KB |
| **SequentialWrite**       | **1000**          | **3,981.475 ms** | **77.7045 ms** |  **68.8830 ms** | **3,973.741 ms** | **3407.76 KB** |
| SequentialObjectWrite | 1000          | 4,125.157 ms | 80.6606 ms | 107.6796 ms | 4,091.991 ms | 5173.31 KB |
| BulkWrite             | 1000          |    10.604 ms |  0.5047 ms |   1.4802 ms |    10.242 ms |  989.19 KB |
| WriteWithExpiration   | 1000          | 3,941.826 ms | 76.6767 ms |  71.7235 ms | 3,940.286 ms | 3431.03 KB |
