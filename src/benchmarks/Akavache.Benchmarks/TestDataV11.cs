// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Benchmarks;

/// <summary>
/// The payload the object benchmarks round-trip through the cache: a small POCO mixing a
/// value type, a string and a timestamp so the measured serialization cost is representative
/// of real cached data rather than of a single primitive. Kept settable because the
/// mixed-operation benchmark reads an entry, mutates it and writes it back.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{Id}")]
public class TestDataV11
{
    /// <summary> Gets or sets the identity the benchmarks compare after a round trip to prove the value came back intact. </summary>
    /// <value>
    /// The identity the benchmarks compare after a round trip.
    /// </value>
    public Guid Id { get; set; }

    /// <summary> Gets or sets the display name, which gives the serializer a variable-length string field to encode. </summary>
    /// <value>
    /// The display name.
    /// </value>
    public string Name { get; set; } = string.Empty;

    /// <summary> Gets or sets the numeric payload the mixed-operation benchmark increments to verify an update round-trips. </summary>
    /// <value>
    /// The numeric payload.
    /// </value>
    public int Value { get; set; }

    /// <summary> Gets or sets the creation timestamp, which gives the serializer a <see cref="DateTimeOffset"/> to encode. </summary>
    /// <value>
    /// The creation timestamp.
    /// </value>
    public DateTimeOffset Created { get; set; }
}
