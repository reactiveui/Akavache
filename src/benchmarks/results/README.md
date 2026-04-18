# Benchmark Comparison: V11 (sqlite-net-pcl) vs V12 (SQLitePCLRaw)

Hardware: AMD Ryzen 7 5800X, .NET 9.0.14, Linux CachyOS

## SqliteBackendBenchmarks — Read Operations

| Method | Size | V11 (sqlite-net-pcl) | V12 (SQLitePCLRaw) | Speedup | V11 Alloc | V12 Alloc | Alloc reduction |
|--------|-----:|---------------------:|-------------------:|--------:|----------:|----------:|----------------:|
| Get_Plain | 1 | 43.62 us | 3.94 us | **11.1x** | 11.15 KB | 1.15 KB | **90%** |
| Get_Plain | 100 | 3,947 us | 355 us | **11.1x** | 1,092 KB | 89 KB | **92%** |
| Get_Plain | 1000 | 39,125 us | 3,549 us | **11.0x** | 10,924 KB | 901 KB | **92%** |
| Get_Encrypted | 1 | 43.35 us | 3.74 us | **11.6x** | 11.03 KB | 1.17 KB | **89%** |
| Get_Encrypted | 100 | 3,926 us | 359 us | **10.9x** | 1,093 KB | 90 KB | **92%** |
| Get_Encrypted | 1000 | 39,617 us | 3,614 us | **11.0x** | 10,920 KB | 900 KB | **92%** |
| BulkGet_Plain | 1 | 45.80 us | 5.69 us | **8.0x** | 11.91 KB | 1.77 KB | **85%** |
| BulkGet_Plain | 100 | 183.67 us | 72.18 us | **2.5x** | 56.35 KB | 45.67 KB | **19%** |
| BulkGet_Plain | 1000 | 1,513 us | 745 us | **2.0x** | 448 KB | 417 KB | **7%** |
| GetAllKeys_Plain | 1 | 46.67 us | 3.51 us | **13.3x** | 10.09 KB | 1.04 KB | **90%** |
| GetAllKeys_Plain | 100 | 123.08 us | 27.11 us | **4.5x** | 43.21 KB | 8.57 KB | **80%** |
| GetAllKeys_Plain | 1000 | 759 us | 206 us | **3.7x** | 328 KB | 72 KB | **78%** |

## SqliteBackendBenchmarks — Write Operations

| Method | Size | V11 (sqlite-net-pcl) | V12 (SQLitePCLRaw) | Speedup | V11 Alloc | V12 Alloc | Alloc reduction |
|--------|-----:|---------------------:|-------------------:|--------:|----------:|----------:|----------------:|
| Insert_Plain | 1 | 3,934 us | 61 us | **64x** | 2.96 KB | 1.26 KB | **57%** |
| Insert_Plain | 100 | 423,081 us | 7,685 us | **55x** | 274 KB | 103 KB | **62%** |
| Insert_Plain | 1000 | 4,232,975 us | 100,964 us | **42x** | 2,735 KB | 1,031 KB | **62%** |
| Insert_Encrypted | 1 | 3,967 us | 97 us | **41x** | 2.96 KB | 1.26 KB | **57%** |
| Insert_Encrypted | 100 | 399,084 us | 12,519 us | **32x** | 274 KB | 103 KB | **62%** |
| Insert_Encrypted | 1000 | 4,758,555 us | 173,942 us | **27x** | 2,735 KB | 1,031 KB | **62%** |
| BulkInsert_Plain | 1 | 3,910 us | 61 us | **64x** | 2.88 KB | 1.18 KB | **59%** |
| BulkInsert_Plain | 100 | 4,385 us | 331 us | **13x** | 26.09 KB | 9.69 KB | **63%** |
| BulkInsert_Plain | 1000 | 10,255 us | 3,052 us | **3.4x** | 237 KB | 87 KB | **63%** |
| Invalidate_Plain | 1 | 7,760 us | 118 us | **66x** | 5.70 KB | 1.92 KB | **66%** |
| Invalidate_Plain | 100 | 398,271 us | 6,233 us | **64x** | 308 KB | 84 KB | **73%** |
| Invalidate_Plain | 1000 | 3,890,885 us | 67,060 us | **58x** | 3,057 KB | 829 KB | **73%** |

## Summary

V12's SQLitePCLRaw direct-access backend delivers:

- **Single-key reads**: ~11x faster, ~90% fewer allocations
- **Key listing**: 4-13x faster, 78-90% fewer allocations
- **Single-key writes**: 27-66x faster, 57-73% fewer allocations
- **Bulk writes at scale**: 3-13x faster, 59-63% fewer allocations

The gains come from eliminating the sqlite-net-pcl ORM layer (expression-tree compilation, reflection-based row mapping) and replacing it with cached prepared statements bound positionally. The dedicated worker thread with commit coalescing further reduces per-op transaction overhead on writes.

## Raw Results

- [baseline-sqlite-net-pcl/](baseline-sqlite-net-pcl/) — V11 baseline results (sqlite-net-pcl ORM)
- [v12-sqlitepclraw/](v12-sqlitepclraw/) — V12 results (direct SQLitePCLRaw access)
