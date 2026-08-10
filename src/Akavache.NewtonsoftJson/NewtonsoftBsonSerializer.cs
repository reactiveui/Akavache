// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.NewtonsoftJson;
#else
namespace Akavache.NewtonsoftJson;
#endif

/// <summary>
/// A Newtonsoft.Json serializer configured for maximum Akavache BSON compatibility.
/// This is a convenience class that configures NewtonsoftSerializer to use BSON format.
/// </summary>
[System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
public class NewtonsoftBsonSerializer : NewtonsoftSerializer
{
    /// <summary>Initializes a new instance of the <see cref="NewtonsoftBsonSerializer"/> class.</summary>
    public NewtonsoftBsonSerializer()
    {
        // Configure for BSON format by default
        UseBsonFormat = true;

        // Default to UTC for maximum compatibility
        ForcedDateTimeKind = DateTimeKind.Utc;
    }
}
