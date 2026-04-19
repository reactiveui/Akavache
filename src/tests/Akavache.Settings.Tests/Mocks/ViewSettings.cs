// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Settings.Core;

namespace Akavache.Settings.Tests;

/// <summary>
/// Test fixture for <see cref="SettingsBase"/>. Uses <see cref="SettingsPropertyHelper{T}"/>
/// for every setting so tests can read via <c>Property.Value</c> (sync), write via
/// <c>Property.Set(v)</c>, and still subscribe reactively — exercising all three shapes
/// the helper supports.
/// </summary>
/// <seealso cref="SettingsBase"/>
public class ViewSettings : SettingsBase
{
    /// <summary>Initializes a new instance of the <see cref="ViewSettings"/> class.</summary>
    public ViewSettings()
        : base(nameof(ViewSettings))
    {
        BoolTest = CreateProperty(true, nameof(BoolTest));
        ByteTest = CreateProperty((byte)123, nameof(ByteTest));
        ShortTest = CreateProperty((short)16, nameof(ShortTest));
        IntTest = CreateProperty(1, nameof(IntTest));
        LongTest = CreateProperty(123456L, nameof(LongTest));
        StringTest = CreateProperty<string?>("TestString", nameof(StringTest));
        FloatTest = CreateProperty(2.2f, nameof(FloatTest));
        DoubleTest = CreateProperty(23.8d, nameof(DoubleTest));
        EnumTest = CreateProperty(EnumTestValue.Option1, nameof(EnumTest));
    }

    /// <summary>Gets the bool test property helper.</summary>
    public SettingsPropertyHelper<bool> BoolTest { get; }

    /// <summary>Gets the byte test property helper.</summary>
    public SettingsPropertyHelper<byte> ByteTest { get; }

    /// <summary>Gets the short test property helper.</summary>
    public SettingsPropertyHelper<short> ShortTest { get; }

    /// <summary>Gets the int test property helper.</summary>
    public SettingsPropertyHelper<int> IntTest { get; }

    /// <summary>Gets the long test property helper.</summary>
    public SettingsPropertyHelper<long> LongTest { get; }

    /// <summary>Gets the string test property helper.</summary>
    public SettingsPropertyHelper<string?> StringTest { get; }

    /// <summary>Gets the float test property helper.</summary>
    public SettingsPropertyHelper<float> FloatTest { get; }

    /// <summary>Gets the double test property helper.</summary>
    public SettingsPropertyHelper<double> DoubleTest { get; }

    /// <summary>Gets the enum test property helper.</summary>
    public SettingsPropertyHelper<EnumTestValue> EnumTest { get; }
}
