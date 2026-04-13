// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Windows.Data;

namespace AkavacheTodoWpf.Converters;

/// <summary>
/// Converts integers to boolean for visibility.
/// </summary>
public class IntToBooleanConverter : IValueConverter
{
    /// <summary>
    /// Converts integer to boolean.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>True if greater than 0.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int intValue)
        {
            return false;
        }

        return intValue > 0;
    }

    /// <summary>
    /// One-way converter — returns <see cref="Binding.DoNothing"/> so the binding engine skips the source update.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns><see cref="Binding.DoNothing"/>.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
