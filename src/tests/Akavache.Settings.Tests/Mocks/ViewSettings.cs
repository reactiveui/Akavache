// Copyright (c) 2019-2026 ReactiveUI and Contributors. All rights reserved.
// ReactiveUI and Contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Settings.Tests;
#else
namespace Akavache.Settings.Tests;
#endif

/// <summary>
/// Test fixture for <see cref="SettingsBase"/>. Uses <see cref="SettingsPropertyHelper{T}"/>
/// for every setting so tests can read via <c>Property.Value</c> (sync), write via
/// <c>Property.Set(v)</c>, and still subscribe reactively — exercising all three shapes
/// the helper supports.
/// </summary>
/// <seealso cref="SettingsBase"/>
[System.Diagnostics.DebuggerDisplay("{BoolTest}")]
public class ViewSettings : SettingsBase
{
    /// <summary>The seeded default for <see cref="ByteTest"/>.</summary>
    private const byte DefaultByteSetting = 123;

    /// <summary>The seeded default for <see cref="ShortTest"/>.</summary>
    private const short DefaultShortSetting = 16;

    /// <summary>The seeded default for <see cref="LongTest"/>.</summary>
    private const long DefaultLongSetting = 123_456L;

    /// <summary>The seeded default for <see cref="FloatTest"/>.</summary>
    private const float DefaultFloatSetting = 2.2F;

    /// <summary>The seeded default for <see cref="DoubleTest"/>.</summary>
    private const double DefaultDoubleSetting = 23.8D;

    /// <summary>Initializes a new instance of the <see cref="ViewSettings"/> class.</summary>
    /// <remarks>
    /// Every call names its property explicitly. <c>CreateProperty</c> keys the backing stream on
    /// <see cref="System.Runtime.CompilerServices.CallerMemberNameAttribute"/>, which resolves to
    /// <c>.ctor</c> for every call made from a constructor — so leaving it implicit here would
    /// give all nine properties the same key and the second one would fail casting the first
    /// one's stream.
    /// </remarks>
    public ViewSettings()
        : base(nameof(ViewSettings))
    {
        BoolTest = CreateProperty(true, nameof(BoolTest));
        ByteTest = CreateProperty(DefaultByteSetting, nameof(ByteTest));
        ShortTest = CreateProperty(DefaultShortSetting, nameof(ShortTest));
        IntTest = CreateProperty(1, nameof(IntTest));
        LongTest = CreateProperty(DefaultLongSetting, nameof(LongTest));
        StringTest = CreateProperty<string?>("TestString", nameof(StringTest));
        FloatTest = CreateProperty(DefaultFloatSetting, nameof(FloatTest));
        DoubleTest = CreateProperty(DefaultDoubleSetting, nameof(DoubleTest));
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
