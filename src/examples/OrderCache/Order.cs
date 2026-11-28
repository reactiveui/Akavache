// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace OrderCache;

/// <summary>An order saved for offline lookup.</summary>
/// <param name="Number">The order number.</param>
/// <param name="Customer">The customer's name.</param>
internal sealed record Order(int Number, string Customer);
