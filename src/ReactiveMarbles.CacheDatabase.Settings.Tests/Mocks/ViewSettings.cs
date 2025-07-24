// Copyright (c) 2019-2022 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ReactiveMarbles.CacheDatabase.Settings.Tests
{
    /// <summary>
    /// View Settings.
    /// </summary>
    /// <seealso cref="ReactiveMarbles.CacheDatabase.Settings.SettingsBase" />
    public class ViewSettings : SettingsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewSettings"/> class.
        /// </summary>
        public ViewSettings()
            : base(nameof(ViewSettings))
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether [bool test].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [bool test]; otherwise, <c>false</c>.
        /// </value>
        public bool BoolTest
        {
            get => GetOrCreate(true); set => SetOrCreate(value);
        }

        /// <summary>
        /// Gets or sets the byte test.
        /// </summary>
        /// <value>
        /// The byte test.
        /// </value>
        public byte ByteTest
        {
            get => GetOrCreate((byte)123); set => SetOrCreate(value);
        }

        /// <summary>
        /// Gets or sets the short test.
        /// </summary>
        /// <value>
        /// The short test.
        /// </value>
        public short ShortTest
        {
            get => GetOrCreate((short)16); set => SetOrCreate(value);
        }

        /// <summary>
        /// Gets or sets the int test.
        /// </summary>
        /// <value>
        /// The int test.
        /// </value>
        public int IntTest
        {
            get => GetOrCreate(1); set => SetOrCreate(value);
        }

        /// <summary>
        /// Gets or sets the long test.
        /// </summary>
        /// <value>
        /// The long test.
        /// </value>
        public long LongTest
        {
            get => GetOrCreate(123456); set => SetOrCreate(value);
        }

        /// <summary>
        /// Gets or sets the string test.
        /// </summary>
        /// <value>
        /// The string test.
        /// </value>
        public string? StringTest
        {
            get => GetOrCreate("TestString"); set => SetOrCreate(value);
        }

        /// <summary>
        /// Gets or sets the float test.
        /// </summary>
        /// <value>
        /// The float test.
        /// </value>
        public float FloatTest
        {
            get => GetOrCreate(2.2f); set => SetOrCreate(value);
        }

        /// <summary>
        /// Gets or sets the double test.
        /// </summary>
        /// <value>
        /// The double test.
        /// </value>
        public double DoubleTest
        {
            get => GetOrCreate(23.8d); set => SetOrCreate(value);
        }

        /// <summary>
        /// Gets or sets the enum test.
        /// </summary>
        /// <value>
        /// The enum test.
        /// </value>
        public EnumTestValue EnumTest
        {
            get => GetOrCreate(EnumTestValue.Option1); set => SetOrCreate(value);
        }
    }
}
