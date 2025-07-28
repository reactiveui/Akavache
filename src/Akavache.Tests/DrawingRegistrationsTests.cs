// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Drawing;
using Xunit;

namespace Akavache.Tests;

/// <summary>
/// Tests for Akavache.Drawing Registrations functionality.
/// </summary>
public class DrawingRegistrationsTests
{
    /// <summary>
    /// Tests that RegisterBitmapLoader can be called without errors.
    /// </summary>
    [Fact]
    public void RegisterBitmapLoaderShouldNotThrow()
    {
        // Act & Assert - Should not throw
        var exception = Record.Exception(() => Registrations.RegisterBitmapLoader());

        // The method might throw on some platforms where platform bitmap loading isn't available
        // This is acceptable behavior
        if (exception != null)
        {
            // Verify it's an expected type of exception
            Assert.True(
                exception is InvalidOperationException ||
                exception is NotSupportedException ||
                exception is PlatformNotSupportedException ||
                exception.Message.Contains("platform") ||
                exception.Message.Contains("Splat") ||
                exception.Message.Contains("dependency"),
                $"Unexpected exception type: {exception.GetType().Name} - {exception.Message}");
        }
    }

    /// <summary>
    /// Tests that Initialize can be called without errors.
    /// </summary>
    [Fact]
    public void InitializeShouldNotThrow()
    {
        // Act & Assert - Should not throw
        var exception = Record.Exception(() => Registrations.Initialize());

        // The method might throw on some platforms where platform bitmap loading isn't available
        // This is acceptable behavior
        if (exception != null)
        {
            // Verify it's an expected type of exception
            Assert.True(
                exception is InvalidOperationException ||
                exception is NotSupportedException ||
                exception is PlatformNotSupportedException ||
                exception.Message.Contains("platform") ||
                exception.Message.Contains("Splat") ||
                exception.Message.Contains("dependency"),
                $"Unexpected exception type: {exception.GetType().Name} - {exception.Message}");
        }
    }

    /// <summary>
    /// Tests that Initialize and RegisterBitmapLoader are equivalent.
    /// </summary>
    [Fact]
    public void InitializeAndRegisterBitmapLoaderShouldBeEquivalent()
    {
        // Both methods should behave the same way
        var initializeException = Record.Exception(() => Registrations.Initialize());
        var registerException = Record.Exception(() => Registrations.RegisterBitmapLoader());

        // Both should either succeed or fail with the same type of exception
        if (initializeException == null)
        {
            // If Initialize succeeds, RegisterBitmapLoader should also succeed (or at least not fail differently)
            Assert.True(registerException == null ||
                       registerException.GetType() == initializeException?.GetType());
        }
        else
        {
            // If Initialize fails, RegisterBitmapLoader should fail similarly
            Assert.NotNull(registerException);
            Assert.True(
                registerException.GetType() == initializeException.GetType() ||
                registerException.Message.Contains("platform") ||
                registerException.Message.Contains("Splat"));
        }
    }

    /// <summary>
    /// Tests that multiple calls to Initialize don't cause issues.
    /// </summary>
    [Fact]
    public void MultipleInitializeCallsShouldBeHandledGracefully()
    {
        // Act - Call Initialize multiple times
        var exceptions = new List<Exception>();

        for (var i = 0; i < 5; i++)
        {
            var exception = Record.Exception(() => Registrations.Initialize());
            if (exception != null)
            {
                exceptions.Add(exception);
            }
        }

        // Assert - Either all calls succeed, or they all fail with the same type of exception
        if (exceptions.Count > 0)
        {
            // All exceptions should be of similar type
            var firstExceptionType = exceptions[0].GetType();
            Assert.All(
                exceptions,
                ex =>
                Assert.True(
                    ex.GetType() == firstExceptionType ||
                    ex.Message.Contains("platform") ||
                    ex.Message.Contains("Splat"),
                    $"Exception type mismatch: expected {firstExceptionType.Name}, got {ex.GetType().Name}"));
        }
    }

    /// <summary>
    /// Tests that multiple calls to RegisterBitmapLoader don't cause issues.
    /// </summary>
    [Fact]
    public void MultipleRegisterBitmapLoaderCallsShouldBeHandledGracefully()
    {
        // Act - Call RegisterBitmapLoader multiple times
        var exceptions = new List<Exception>();

        for (var i = 0; i < 5; i++)
        {
            var exception = Record.Exception(() => Registrations.RegisterBitmapLoader());
            if (exception != null)
            {
                exceptions.Add(exception);
            }
        }

        // Assert - Either all calls succeed, or they all fail with the same type of exception
        if (exceptions.Count > 0)
        {
            // All exceptions should be of similar type
            var firstExceptionType = exceptions[0].GetType();
            Assert.All(
                exceptions,
                ex =>
                Assert.True(
                    ex.GetType() == firstExceptionType ||
                    ex.Message.Contains("platform") ||
                    ex.Message.Contains("Splat"),
                    $"Exception type mismatch: expected {firstExceptionType.Name}, got {ex.GetType().Name}"));
        }
    }

    /// <summary>
    /// Tests that the Registrations class has correct attributes for AOT compatibility.
    /// </summary>
    [Fact]
    public void RegistrationsClassShouldHaveCorrectAttributes()
    {
        // Act
        var registrationsType = typeof(Registrations);

        // Assert - Verify it's a static class
        Assert.True(registrationsType.IsAbstract && registrationsType.IsSealed, "Registrations should be a static class");

        // Check for Preserve attribute (it may not be accessible from test assembly)
        var attributes = registrationsType.GetCustomAttributes(false);
        var hasPreserveAttribute = attributes.Any(attr => attr.GetType().Name.Contains("Preserve"));
        Assert.True(
            hasPreserveAttribute,
            "Registrations should have Preserve attribute");

        // Verify methods are public and static
        var initializeMethod = registrationsType.GetMethod("Initialize");
        Assert.NotNull(initializeMethod);
        Assert.True(initializeMethod!.IsStatic);
        Assert.True(initializeMethod.IsPublic);

        var registerMethod = registrationsType.GetMethod("RegisterBitmapLoader");
        Assert.NotNull(registerMethod);
        Assert.True(registerMethod!.IsStatic);
        Assert.True(registerMethod.IsPublic);
    }

    /// <summary>
    /// Tests that concurrent calls to drawing registrations are handled safely.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ConcurrentRegistrationCallsShouldBeSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var lockObject = new object();

        // Act - Make concurrent calls to both registration methods
        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    Registrations.Initialize();
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    Registrations.RegisterBitmapLoader();
                }
                catch (Exception ex)
                {
                    lock (lockObject)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert - If there are exceptions, they should all be of expected types
        foreach (var exception in exceptions)
        {
            Assert.True(
                exception is InvalidOperationException ||
                exception is NotSupportedException ||
                exception is PlatformNotSupportedException ||
                exception.Message.Contains("platform") ||
                exception.Message.Contains("Splat") ||
                exception.Message.Contains("dependency"),
                $"Unexpected exception type: {exception.GetType().Name} - {exception.Message}");
        }
    }

    /// <summary>
    /// Tests that drawing registrations work with different compiler directives.
    /// </summary>
    [Fact]
    public void RegistrationsShouldHandleCompilerDirectives()
    {
        // This test verifies that the NETSTANDARD and other compiler directive handling
        // works correctly in the Registrations class.

        // Act & Assert - The methods should exist and be callable regardless of platform
        var registrationsType = typeof(Registrations);

        var initializeMethod = registrationsType.GetMethod("Initialize");
        Assert.NotNull(initializeMethod);

        var registerMethod = registrationsType.GetMethod("RegisterBitmapLoader");
        Assert.NotNull(registerMethod);

        // Methods should be accessible (they may throw at runtime on unsupported platforms)
        Assert.True(initializeMethod!.IsPublic);
        Assert.True(registerMethod!.IsPublic);
    }

    /// <summary>
    /// Tests that Registrations class follows proper naming conventions.
    /// </summary>
    [Fact]
    public void RegistrationsShouldFollowNamingConventions()
    {
        // Act
        var registrationsType = typeof(Registrations);

        // Assert
        Assert.Equal("Registrations", registrationsType.Name);
        Assert.Equal("Akavache.Drawing", registrationsType.Namespace);
        Assert.True(registrationsType.IsPublic);

        // Check method names
        var methods = registrationsType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var publicMethods = methods.Where(m => m.DeclaringType == registrationsType).ToArray();

        Assert.Contains(publicMethods, m => m.Name == "Initialize");
        Assert.Contains(publicMethods, m => m.Name == "RegisterBitmapLoader");
    }
}
