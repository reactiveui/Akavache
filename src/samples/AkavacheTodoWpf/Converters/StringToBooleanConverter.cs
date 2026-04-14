// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Windows.Data;

namespace AkavacheTodoWpf.Converters;

/// <summary>
/// Converts string to boolean for visibility.
/// </summary>
public class StringToBooleanConverter : IValueConverter
{
    /// <summary>
    /// Converts string to boolean.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>True if string is not null or empty.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => !string.IsNullOrWhiteSpace(value?.ToString());

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
