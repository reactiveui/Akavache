// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Core;
using Akavache.NewtonsoftJson;
using Akavache.SystemTextJson;
using Xunit;

namespace Akavache.Tests;

/// <summary>
/// Tests for CoreRegistrations functionality and global configuration.
/// </summary>
public class CoreRegistrationsTests
{
    /// <summary>
    /// Tests that CoreRegistrations.Serializer can be set and retrieved correctly.
    /// </summary>
    [Fact]
    public void SerializerRegistrationShouldWorkCorrectly()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        var testSerializers = new ISerializer[]
        {
            new SystemJsonSerializer(),
            new SystemJsonBsonSerializer(),
            new NewtonsoftSerializer(),
            new NewtonsoftBsonSerializer()
        };

        try
        {
            foreach (var testSerializer in testSerializers)
            {
                // Act
                CoreRegistrations.Serializer = testSerializer;

                // Assert
                Assert.Same(testSerializer, CoreRegistrations.Serializer);
                Assert.NotNull(CoreRegistrations.Serializer);
            }

            // Test setting to null
            CoreRegistrations.Serializer = null;
            Assert.Null(CoreRegistrations.Serializer);
        }
        finally
        {
            // Restore original serializer
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that CoreRegistrations.TaskpoolScheduler is available and functional.
    /// </summary>
    [Fact]
    public void TaskpoolSchedulerShouldBeAvailable()
    {
        // Act
        var scheduler = CoreRegistrations.TaskpoolScheduler;

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
    /// Tests that CoreRegistrations.HttpService is available and functional.
    /// </summary>
    [Fact]
    public void HttpServiceShouldBeAvailable()
    {
        // Act
        var httpService = CoreRegistrations.HttpService;

        // Assert
        Assert.NotNull(httpService);

        // Test that it's a valid HttpService instance
        Assert.IsType<HttpService>(httpService);
    }

    /// <summary>
    /// Tests that multiple serializer registrations work correctly in sequence.
    /// </summary>
    [Fact]
    public void MultipleSerializerRegistrationsShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;

        try
        {
            // Test rapid serializer changes
            var serializers = new ISerializer[]
            {
                new SystemJsonSerializer(),
                new NewtonsoftSerializer(),
                new SystemJsonBsonSerializer(),
                new NewtonsoftBsonSerializer()
            };

            for (var i = 0; i < 10; i++)
            {
                foreach (var serializer in serializers)
                {
                    // Act
                    CoreRegistrations.Serializer = serializer;

                    // Assert
                    Assert.Same(serializer, CoreRegistrations.Serializer);

                    // Test that serializer actually works
                    var testData = $"test_data_{i}_{serializer.GetType().Name}";
                    var serialized = serializer.Serialize(testData);
                    var deserialized = serializer.Deserialize<string>(serialized);
                    Assert.Equal(testData, deserialized);
                }
            }
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that CoreRegistrations handles concurrent access correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ConcurrentAccessShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;

        try
        {
            var serializers = new ISerializer[]
            {
                new SystemJsonSerializer(),
                new NewtonsoftSerializer(),
                new SystemJsonBsonSerializer(),
                new NewtonsoftBsonSerializer()
            };

            // Act - Multiple threads accessing CoreRegistrations concurrently
            var tasks = new List<Task>();
            var exceptions = new List<Exception>();

            for (var i = 0; i < 20; i++)
            {
                var taskIndex = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var serializer = serializers[taskIndex % serializers.Length];

                        // Set serializer
                        CoreRegistrations.Serializer = serializer;

                        // Get serializer
                        var retrieved = CoreRegistrations.Serializer;

                        // Use serializer
                        if (retrieved != null)
                        {
                            var testData = $"concurrent_test_{taskIndex}";
                            var serialized = retrieved.Serialize(testData);
                            var deserialized = retrieved.Deserialize<string>(serialized);
                            Assert.Equal(testData, deserialized);
                        }

                        // Access other registrations
                        var scheduler = CoreRegistrations.TaskpoolScheduler;
                        Assert.NotNull(scheduler);

                        var httpService = CoreRegistrations.HttpService;
                        Assert.NotNull(httpService);
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Assert - No exceptions should have occurred
            Assert.Empty(exceptions);
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that CoreRegistrations properly validates serializer functionality.
    /// </summary>
    [Fact]
    public void SerializerFunctionalityValidationShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;

        try
        {
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
                // Act
                CoreRegistrations.Serializer = serializer;

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
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that CoreRegistrations handles null serializer scenarios correctly.
    /// </summary>
    [Fact]
    public void NullSerializerHandlingShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;

        try
        {
            // Act - Set serializer to null
            CoreRegistrations.Serializer = null;

            // Assert
            Assert.Null(CoreRegistrations.Serializer);

            // Other registrations should still work
            Assert.NotNull(CoreRegistrations.TaskpoolScheduler);
            Assert.NotNull(CoreRegistrations.HttpService);

            // Restore a valid serializer
            CoreRegistrations.Serializer = new SystemJsonSerializer();
            Assert.NotNull(CoreRegistrations.Serializer);
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that CoreRegistrations serializer configuration persists correctly.
    /// </summary>
    [Fact]
    public void SerializerConfigurationPersistenceShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;

        try
        {
            // Test SystemJsonSerializer configuration
            var systemJsonSerializer = new SystemJsonSerializer();
            systemJsonSerializer.ForcedDateTimeKind = DateTimeKind.Utc;

            CoreRegistrations.Serializer = systemJsonSerializer;

            var retrieved = CoreRegistrations.Serializer;
            Assert.Same(systemJsonSerializer, retrieved);
            Assert.Equal(DateTimeKind.Utc, retrieved.ForcedDateTimeKind);

            // Test NewtonsoftSerializer configuration
            var newtonsoftSerializer = new NewtonsoftSerializer();
            newtonsoftSerializer.ForcedDateTimeKind = DateTimeKind.Local;

            CoreRegistrations.Serializer = newtonsoftSerializer;

            retrieved = CoreRegistrations.Serializer;
            Assert.Same(newtonsoftSerializer, retrieved);
            Assert.Equal(DateTimeKind.Local, retrieved.ForcedDateTimeKind);
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that CoreRegistrations handles serializer disposal scenarios correctly.
    /// </summary>
    [Fact]
    public void SerializerDisposalScenariosShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;

        try
        {
            // Create a disposable serializer
            var serializer = new SystemJsonSerializer();
            CoreRegistrations.Serializer = serializer;

            // Verify it's set
            Assert.Same(serializer, CoreRegistrations.Serializer);

            // Replace with another serializer
            var newSerializer = new NewtonsoftSerializer();
            CoreRegistrations.Serializer = newSerializer;

            // Verify replacement
            Assert.Same(newSerializer, CoreRegistrations.Serializer);
            Assert.NotSame(serializer, CoreRegistrations.Serializer);

            // Test that the new serializer works
            var testData = "disposal_test";
            var serialized = newSerializer.Serialize(testData);
            var deserialized = newSerializer.Deserialize<string>(serialized);
            Assert.Equal(testData, deserialized);
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
        }
    }

    /// <summary>
    /// Tests that CoreRegistrations maintains thread safety.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ThreadSafetyShouldWork()
    {
        // Arrange
        var originalSerializer = CoreRegistrations.Serializer;
        var exceptions = new List<Exception>();
        var barrier = new Barrier(Environment.ProcessorCount);

        try
        {
            var tasks = new List<Task>();

            // Create multiple threads that simultaneously access and modify CoreRegistrations
            for (var i = 0; i < Environment.ProcessorCount; i++)
            {
                var threadIndex = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        barrier.SignalAndWait(); // Synchronize start

                        for (var j = 0; j < 100; j++)
                        {
                            // Alternate between different operations
                            if (j % 4 == 0)
                            {
                                CoreRegistrations.Serializer = new SystemJsonSerializer();
                            }
                            else if (j % 4 == 1)
                            {
                                var serializer = CoreRegistrations.Serializer;
                                Assert.NotNull(serializer);
                            }
                            else if (j % 4 == 2)
                            {
                                var scheduler = CoreRegistrations.TaskpoolScheduler;
                                Assert.NotNull(scheduler);
                            }
                            else
                            {
                                var httpService = CoreRegistrations.HttpService;
                                Assert.NotNull(httpService);
                            }

                            // Small delay to increase chance of race conditions
                            Thread.Sleep(1);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Assert - Should complete without exceptions
            Assert.Empty(exceptions);
        }
        finally
        {
            CoreRegistrations.Serializer = originalSerializer;
            barrier.Dispose();
        }
    }
}
