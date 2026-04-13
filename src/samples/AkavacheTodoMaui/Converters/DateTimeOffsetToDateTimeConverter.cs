// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Globalization;

namespace AkavacheTodoMaui.Converters;

/// <summary>
/// Converts DateTimeOffset to DateTime for DatePicker binding.
/// </summary>
public class DateTimeOffsetToDateTimeConverter : IValueConverter
{
    /// <summary>
    /// Converts DateTimeOffset to DateTime.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The converted value.</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.DateTime;
        }

        return DateTime.Today;
    }

    /// <summary>
    /// Converts DateTime back to DateTimeOffset.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="targetType">The target type.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="culture">The culture.</param>
    /// <returns>The converted value.</returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime)
        {
            return null;
        }

        return new DateTimeOffset(dateTime);
    }
}
