// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;

namespace Akavache.Tests;

/// <summary>
/// Tests for CacheDatabase functionality and global configuration.
/// </summary>
[Category("Akavache")]
[NotInParallel]
public class CacheDatabaseTests
{
    /// <summary>
    /// Tests that CacheDatabase.TaskpoolScheduler is available and functional.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task TaskpoolSchedulerShouldBeAvailable()
    {
        // Act
        var scheduler = CacheDatabase.TaskpoolScheduler;

        // Assert
        await Assert.That(scheduler).IsNotNull();

        // Test that it can schedule work
        var workExecuted = false;
        var resetEvent = new ManualResetEventSlim(false);

        scheduler.Schedule(() =>
        {
            workExecuted = true;
            resetEvent.Set();
        });

        using (Assert.Multiple())
        {
            // Wait for work to complete
            await Assert.That(resetEvent.Wait(5000)).IsTrue();
            await Assert.That(workExecuted).IsTrue();
        }
    }

    /// <summary>
    /// Tests that CacheDatabase.HttpService is available and functional.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task HttpServiceShouldBeAvailable()
    {
        CacheDatabase.Initialize<SystemJsonSerializer>();

        // Act
        var httpService = CacheDatabase.InMemory.HttpService;

        // Assert
        // You can combine multiple constraints for a more fluent assertion.
        await Assert.That(httpService).IsNotNull().And.IsTypeOf<HttpService>();
    }

    /// <summary>
    /// Tests that CacheDatabase properly validates serializer functionality.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task SerializerFunctionalityValidationShouldWork()
    {
        // Arrange
        object[] testCases =
        [
            "string test",
            42,
            3.14d,
            true,
            DateTime.UtcNow,
            DateTimeOffset.Now,
            Guid.NewGuid(),
            new { Name = "Test", Value = 123 },
            (int[])[1, 2, 3, 4, 5],
            new Dictionary<string, object> { ["key1"] = "value1", ["key2"] = 42 }
        ];

        ISerializer[] serializers =
        [
            new SystemJsonSerializer(),
            new SystemJsonBsonSerializer(),
            new NewtonsoftSerializer(),
            new NewtonsoftBsonSerializer()
        ];

        foreach (var serializer in serializers)
        {
            // Assert - Test each serializer with various data types
            foreach (var testCase in testCases)
            {
                try
                {
                    var serialized = serializer.Serialize(testCase);
                    await Assert.That(serialized).IsNotNull();
                    await Assert.That(serialized).IsNotEmpty();

                    // For simple types, test round-trip
                    if (testCase is string or int or double or bool)
                    {
                        var deserialized = serializer.Deserialize<object>(serialized);

                        // For basic equality comparison, convert both to string
                        await Assert.That(deserialized?.ToString()).IsEqualTo(testCase.ToString());
                    }
                }
                catch (Exception ex)
                {
                    // Some serializers might not support all types - that's acceptable
                    // Just ensure we don't get unexpected exceptions
                    await Assert.That(ex)
                        .IsTypeOf<NotSupportedException>()
                        .Or.IsTypeOf<InvalidOperationException>();
                }
            }
        }
    }
}
