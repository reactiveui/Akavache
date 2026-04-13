// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;

namespace AkavacheTodoMaui.Converters;

/// <summary>
/// Converts integer to boolean for visibility binding.
/// </summary>
public class IntToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts integer to boolean.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>True if integer is greater than 0.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is > 0;

    /// <summary>
    /// One-way converter — returns <see cref="BindableProperty.UnsetValue"/> so the binding engine leaves the source untouched.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns><see cref="BindableProperty.UnsetValue"/>.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => BindableProperty.UnsetValue;
}
