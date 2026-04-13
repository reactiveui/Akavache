// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.NewtonsoftJson;

/// <summary>
/// A Newtonsoft.Json serializer configured for maximum Akavache BSON compatibility.
/// This is a convenience class that configures NewtonsoftSerializer to use BSON format.
/// </summary>
public class NewtonsoftBsonSerializer : NewtonsoftSerializer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewtonsoftBsonSerializer"/> class.
    /// </summary>
    public NewtonsoftBsonSerializer()
    {
        // Configure for BSON format by default
        UseBsonFormat = true;

        // Default to UTC for maximum compatibility
        ForcedDateTimeKind = DateTimeKind.Utc;
    }
}
