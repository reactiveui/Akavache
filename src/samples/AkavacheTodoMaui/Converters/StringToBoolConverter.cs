// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;

namespace AkavacheTodoMaui.Converters;

/// <summary>
/// Converts string to boolean for visibility binding.
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    /// <summary>
    /// Converts string to boolean.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>True if string is not null or empty.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => !string.IsNullOrWhiteSpace(value?.ToString());

    /// <summary>
    /// Not implemented for one-way binding.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>Not supported.</returns>
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
