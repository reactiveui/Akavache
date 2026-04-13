// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;
using System.Windows.Data;

namespace AkavacheTodoWpf.Converters;

/// <summary>
/// Inverts boolean values.
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    /// <summary>
    /// Inverts boolean value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>Inverted boolean.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
        {
            return true;
        }

        return !boolValue;
    }

    /// <summary>
    /// Inverts boolean value back.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>Inverted boolean.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
        {
            return false;
        }

        return !boolValue;
    }
}
