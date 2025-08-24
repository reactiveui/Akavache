// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Xunit;

namespace Akavache.Tests;

/// <summary>
/// Tests for CacheDatabase functionality and global configuration.
/// </summary>
public class CacheDatabaseTests
{
    /// <summary>
    /// Tests that CacheDatabase.TaskpoolScheduler is available and functional.
    /// </summary>
    [Fact]
    public void TaskpoolSchedulerShouldBeAvailable()
    {
        // Act
        var scheduler = CacheDatabase.TaskpoolScheduler;

        // Assert
        Assert.NotNull(scheduler);

        // Test that it can schedule work
        var workExecuted = false;
        var resetEvent = new ManualResetEventSlim(false);

        scheduler.Schedule(() =>
        {
            workExecuted = true;
            resetEvent.Set();
        });

        // Wait for work to complete
        Assert.True(resetEvent.Wait(5000), "Scheduled work did not complete within timeout");
        Assert.True(workExecuted);
    }

    /// <summary>
    /// Tests that CacheDatabase.HttpService is available and functional.
    /// </summary>
    [Fact]
    public void HttpServiceShouldBeAvailable()
    {
        CacheDatabase.Initialize(new SystemJsonSerializer());

        // Act
        var httpService = CacheDatabase.HttpService;

        // Assert
        Assert.NotNull(httpService);

        // Test that it's a valid HttpService instance
        Assert.IsType<HttpService>(httpService);
    }

    /// <summary>
    /// Tests that CacheDatabase properly validates serializer functionality.
    /// </summary>
    [Fact]
    public void SerializerFunctionalityValidationShouldWork()
    {
        // Arrange
        var testCases = new object[]
        {
                "string test",
                42,
                3.14d,
                true,
                DateTime.UtcNow,
                DateTimeOffset.Now,
                Guid.NewGuid(),
                new { Name = "Test", Value = 123 },
                new[] { 1, 2, 3, 4, 5 },
                new Dictionary<string, object> { ["key1"] = "value1", ["key2"] = 42 }
        };

        var serializers = new ISerializer[]
        {
                new SystemJsonSerializer(),
                new SystemJsonBsonSerializer(),
                new NewtonsoftSerializer(),
                new NewtonsoftBsonSerializer()
        };

        foreach (var serializer in serializers)
        {
            // Assert - Test each serializer with various data types
            foreach (var testCase in testCases)
            {
                try
                {
                    var serialized = serializer.Serialize(testCase);
                    Assert.NotNull(serialized);
                    Assert.True(serialized.Length > 0);

                    // For simple types, test round-trip
                    if (testCase is string || testCase is int || testCase is double || testCase is bool)
                    {
                        var deserialized = serializer.Deserialize<object>(serialized);

                        // For basic equality comparison, convert both to string
                        Assert.Equal(testCase.ToString(), deserialized?.ToString());
                    }
                }
                catch (Exception ex)
                {
                    // Some serializers might not support all types - that's acceptable
                    // Just ensure we don't get unexpected exceptions
                    Assert.True(
                        ex is NotSupportedException || ex is InvalidOperationException,
                        $"Unexpected exception type {ex.GetType().Name} for serializer {serializer.GetType().Name} with data type {testCase.GetType().Name}: {ex.Message}");
                }
            }
        }
    }
}
