// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Akavache.Helpers;

/// <summary>
/// Provides helper methods for argument validation. These methods serve as polyfills
/// for <c>ArgumentNullException.ThrowIfNull</c> and related members that are only
/// available on newer .NET versions, allowing the same call-site pattern to work
/// regardless of the target framework.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ArgumentExceptionHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.
    /// </summary>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is not null)
        {
            return;
        }

        throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Validates that <paramref name="argument"/> is neither null nor composed entirely
    /// of white-space characters.
    /// </summary>
    /// <remarks>
    /// Throws <see cref="ArgumentNullException"/> when <paramref name="argument"/> is
    /// <see langword="null"/>, and <see cref="ArgumentException"/> when it is empty or
    /// whitespace-only. Mirrors the exception shape of the
    /// <c>ArgumentException.ThrowIfNullOrWhiteSpace</c> BCL helper added in .NET 8.
    /// </remarks>
    /// <param name="argument">The string argument to validate.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="argument"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="argument"/> is empty or whitespace.</exception>
    public static void ThrowIfNullOrWhiteSpace([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (!string.IsNullOrWhiteSpace(argument))
        {
            return;
        }

        throw new ArgumentException("The value cannot be empty or composed entirely of whitespace.", paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> with the supplied message and parameter name.
    /// Centralises the throw site so call-sites read as a single statement instead of an
    /// inline <c>throw new ArgumentException(...)</c>.
    /// </summary>
    /// <param name="message">The validation message describing why the argument is invalid.</param>
    /// <param name="paramName">The parameter name to attach to the exception.</param>
    /// <exception cref="ArgumentException">Always thrown.</exception>
    [DoesNotReturn]
    public static void ThrowArgument(string message, string paramName) =>
        throw new ArgumentException(message, paramName);

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> when <paramref name="condition"/> is
    /// <see langword="true"/>. Lets validation call-sites collapse to a single line
    /// (<c>ArgumentExceptionHelper.ThrowArgumentIf(name.IsBad(), "...", nameof(name))</c>)
    /// instead of an explicit <c>if</c>/<c>throw</c> block.
    /// </summary>
    /// <param name="condition">When <see langword="true"/>, the exception is thrown.</param>
    /// <param name="message">The validation message describing why the argument is invalid.</param>
    /// <param name="paramName">The parameter name to attach to the exception.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="condition"/> is <see langword="true"/>.</exception>
    public static void ThrowArgumentIf(bool condition, string message, string paramName)
    {
        if (!condition)
        {
            return;
        }

        throw new ArgumentException(message, paramName);
    }
}
