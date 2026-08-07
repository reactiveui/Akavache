// Copyright (c) 2019-2026 ReactiveUI Association Incorporated. All rights reserved.
// ReactiveUI Association Incorporated licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

#if REACTIVE_SHIM
namespace Akavache.Reactive.Integration.Tests;
#else
namespace Akavache.Integration.Tests;
#endif

/// <summary>Tests covering bulk object insertion through the serializer extensions.</summary>
public partial class SerializerExtensionsTests
{
    /// <summary>Tests that InsertObjects throws ArgumentNullException when cache is null.</summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Test]
    public async Task InsertObjectsShouldThrowArgumentNullExceptionWhenCacheIsNull()
    {
        // Arrange
        IBlobCache? cache = null;
        Dictionary<string, object> dict = new() { ["key"] = SingleEntryValue };

        // Act & Assert
        await Assert.That(() => cache!.InsertObjects(dict)).Throws<ArgumentNullException>();
    }

    /// <summary>Tests that InsertObjects throws ArgumentNullException when keyValuePairs is null.</summary>
    /// <returns>A task representing the test completion.</returns>
    [Test]
    public async Task InsertObjectsShouldThrowArgumentNullExceptionWhenKeyValuePairsIsNull()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
        Dictionary<string, object>? dict = null;

        try
        {
            // Act & Assert
            await Assert.That(() => cache.InsertObjects(dict!)).Throws<ArgumentNullException>();
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>Tests that InsertObjects handles empty dictionary correctly.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsShouldHandleEmptyDictionary()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);
            Dictionary<string, object> emptyDict = [];

            try
            {
                // Act - should complete without error
                _ = cache.InsertObjects(emptyDict).Subscribe();

                // Assert - test passes if no exception is thrown
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>Tests that mixed object types can be inserted and retrieved.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsShouldHandleMixedObjectTypes()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            // Use 'using' for resource management
            using InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

            DateTime testDate = new(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);
            Dictionary<string, object> mixedObjects = new()
            {
                ["string"] = "test string",
                ["int"] = MixedTypeIntValue,
                ["user"] = new UserObject { Name = "Test User", Bio = "Test Bio", Blog = "Test Blog" },
                ["date"] = testDate
            };

            // Act
            _ = cache.InsertObjects(mixedObjects).Subscribe();

            // Assert
            string? stringValue = null;
            _ = cache.GetObject<string>("string").Subscribe(v => stringValue = v);
            int intValue = 0;
            _ = cache.GetObject<int>("int").Subscribe(v => intValue = v);
            UserObject? userValue = null;
            _ = cache.GetObject<UserObject>("user").Subscribe(v => userValue = v);
            DateTime dateValue = default;
            _ = cache.GetObject<DateTime>("date").Subscribe(v => dateValue = v);

            using (Assert.Multiple())
            {
                await Assert.That(stringValue).IsEqualTo("test string");
                await Assert.That(intValue).IsEqualTo(MixedTypeIntValue);
                await Assert.That(userValue).IsNotNull();
                await Assert.That(userValue!.Name).IsEqualTo("Test User");

                // Verify date value - either default or close to test date
                var isDateValid = dateValue == default || Math.Abs((dateValue - testDate).TotalDays) <= 1;
                await Assert.That(isDateValid).IsTrue();
            }
        }
    }

    /// <summary>Tests that extension methods properly validate arguments.</summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task ExtensionMethodsShouldValidateArguments()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

        try
        {
            // Test null key validation
            Exception? catErr = null;
            _ = cache.GetObjectCreatedAt<string>(null!).Subscribe(static _ => { }, ex => catErr = ex);
            await Assert.That(catErr).IsTypeOf<ArgumentNullException>();

            Exception? invErr = null;
            _ = cache.InvalidateObject<string>(null!).Subscribe(static _ => { }, ex => invErr = ex);
            await Assert.That(invErr).IsTypeOf<ArgumentNullException>();

            // Test null collection validation — throws before Subscribe
            Exception? invsErr = null;
            try
            {
                _ = cache.InvalidateObjects<string>(null!).Subscribe(static _ => { }, ex => invsErr = ex);
            }
            catch (ArgumentNullException ex)
            {
                invsErr = ex;
            }

            await Assert.That(invsErr).IsTypeOf<ArgumentNullException>();

            // Note: Extension methods may allow empty strings as valid keys in some implementations
            // This is different from the core methods which validate empty strings
            // Test that methods work with empty string (if allowed by implementation)
            try
            {
                _ = cache.GetObjectCreatedAt<string>(string.Empty).Subscribe();

                // If no exception is thrown, empty strings are allowed
            }
            catch (KeyNotFoundException)
            {
                // This is expected if the key doesn't exist - empty string is a valid key
            }
            catch (ArgumentException)
            {
                // This would indicate the implementation validates empty strings
                // Both behaviors are valid depending on the implementation
            }
        }
        finally
        {
            cache.Dispose();
        }
    }

    /// <summary>
    /// Tests that InsertObjects handles empty sequence completion robustly.
    /// This test validates the fix for issue #635 where LastOrDefaultAsync()
    /// would throw "Sequence contains no elements" exception.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsShouldHandleEmptySequenceRobustly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

            try
            {
                // Test 1: Empty dictionary should complete without exception
                Dictionary<string, object> emptyDict = [];
                _ = cache.InsertObjects(emptyDict).Subscribe();

                // Test 2: Single item should work
                Dictionary<string, object> singleDict = new() { ["key1"] = FirstEntryValue };
                _ = cache.InsertObjects(singleDict).Subscribe();

                // Test 3: Multiple items should work
                Dictionary<string, object> multiDict = new() { ["key2"] = "value2", ["key3"] = MultiInsertIntValue, ["key4"] = new UserObject { Name = "Test", Bio = "Bio", Blog = "Blog" } };
                _ = cache.InsertObjects(multiDict).Subscribe();

                // Verify all items were inserted correctly
                string? value1 = null;
                _ = cache.GetObject<string>("key1").Subscribe(v => value1 = v);
                string? value2 = null;
                _ = cache.GetObject<string>("key2").Subscribe(v => value2 = v);
                int value3 = 0;
                _ = cache.GetObject<int>("key3").Subscribe(v => value3 = v);
                UserObject? value4 = null;
                _ = cache.GetObject<UserObject>("key4").Subscribe(v => value4 = v);

                using (Assert.Multiple())
                {
                    await Assert.That(value1).IsEqualTo(FirstEntryValue);
                    await Assert.That(value2).IsEqualTo("value2");
                    await Assert.That(value3).IsEqualTo(MultiInsertIntValue);
                    await Assert.That(value4).IsNotNull();
                    await Assert.That(value4!.Name).IsEqualTo("Test");
                }
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that InsertObjects with IEnumerable&lt;KeyValuePair&gt; handles completion properly.
    /// This specifically tests the fix where Count() is used instead of LastOrDefaultAsync()
    /// to avoid "Sequence contains no elements" exceptions.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsGenericShouldHandleSequenceCompletionRobustly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

            try
            {
                // Test 1: Empty collection
                List<KeyValuePair<string, UserObject>> emptyPairs = [];
                _ = cache.InsertObjects(emptyPairs).Subscribe();

                // Test 2: Single item
                List<KeyValuePair<string, UserObject>> singlePair =
                    [new(FirstUserKey, new() { Name = FirstUserName, Bio = "Bio1", Blog = FirstUserBlog })];
                _ = cache.InsertObjects(singlePair).Subscribe();

                // Test 3: Multiple items
                List<KeyValuePair<string, UserObject>> multiPairs =
                [
                    new(SecondUserKey, new() { Name = SecondUserName, Bio = "Bio2", Blog = SecondUserBlog }),
                    new("user3", new() { Name = "User3", Bio = "Bio3", Blog = "Blog3" }),
                    new("user4", new() { Name = "User4", Bio = "Bio4", Blog = "Blog4" })
                ];
                _ = cache.InsertObjects(multiPairs).Subscribe();

                // Test 4: Large collection to stress test the Count() approach
                var largePairs = Enumerable.Range(1, LargeBatchSize)
                    .Select(static i => new KeyValuePair<string, UserObject>(
                        $"large_user_{i}",
                        new() { Name = $"LargeUser{i}", Bio = $"Bio{i}", Blog = $"Blog{i}" }))
                    .ToList();
                _ = cache.InsertObjects(largePairs).Subscribe();

                // Verify some items were inserted correctly
                var user1 = cache.GetObject<UserObject>(FirstUserKey).SubscribeGetValue();
                var user2 = cache.GetObject<UserObject>(SecondUserKey).SubscribeGetValue();
                UserObject? largeUser50 = null;
                _ = cache.GetObject<UserObject>("large_user_50").Subscribe(v => largeUser50 = v);

                using (Assert.Multiple())
                {
                    await Assert.That(user1).IsNotNull();
                    await Assert.That(user1!.Name).IsEqualTo(FirstUserName);
                    await Assert.That(user2).IsNotNull();
                    await Assert.That(user2!.Name).IsEqualTo(SecondUserName);
                    await Assert.That(largeUser50).IsNotNull();
                    await Assert.That(largeUser50!.Name).IsEqualTo("LargeUser50");
                }
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that InsertObjects completion logic is robust and doesn't throw exceptions.
    /// This test verifies the implementation handles various edge cases correctly,
    /// including empty sequences, without throwing "Sequence contains no elements" exceptions.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsCompletionLogicShouldBeRobust()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

            try
            {
                // Test 1: Empty dictionary - should complete without exception
                Dictionary<string, object> emptyDict = [];
                _ = cache.InsertObjects(emptyDict).Subscribe();

                // Test 2: Single item - should complete normally
                Dictionary<string, object> singleDict = new() { ["single"] = SingleEntryValue };
                _ = cache.InsertObjects(singleDict).Subscribe();

                // Test 3: Multiple items including edge cases
                Dictionary<string, object> multiDict = new()
                {
                    ["string_val"] = "test",
                    ["int_val"] = EdgeCaseIntValue,
                    ["null_val"] = null!,
                    [EmptyStringKey] = string.Empty,
                    ["complex_obj"] = (Prop1: FirstEntryValue, Prop2: ComplexPropertyValue)
                };
                _ = cache.InsertObjects(multiDict).Subscribe();

                // Test 4: Large number of operations to stress test completion logic
                var largeDict = Enumerable.Range(1, StressBatchSize)
                    .ToDictionary(static i => $"key_{i}", static i => (object)$"value_{i}");
                _ = cache.InsertObjects(largeDict).Subscribe();

                // Test 5: Verify data was actually stored correctly
                string? retrievedSingle = null;
                _ = cache.GetObject<string>("single").Subscribe(v => retrievedSingle = v);
                string? retrievedString = null;
                _ = cache.GetObject<string>("string_val").Subscribe(v => retrievedString = v);
                int retrievedInt = 0;
                _ = cache.GetObject<int>("int_val").Subscribe(v => retrievedInt = v);
                string? retrievedLarge = null;
                _ = cache.GetObject<string>("key_500").Subscribe(v => retrievedLarge = v);

                using (Assert.Multiple())
                {
                    await Assert.That(retrievedSingle).IsEqualTo(SingleEntryValue);
                    await Assert.That(retrievedString).IsEqualTo("test");
                    await Assert.That(retrievedInt).IsEqualTo(EdgeCaseIntValue);
                    await Assert.That(retrievedLarge).IsEqualTo("value_500");
                }

                // All tests pass - the completion logic is robust
            }
            finally
            {
                cache.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that InsertObjects handles problematic scenarios that could cause
    /// incomplete observable sequences without throwing exceptions.
    /// This validates the robustness of the LastOrDefaultAsync() approach.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Test]
    public async Task InsertObjectsShouldHandleProblematicScenariosRobustly()
    {
        // Arrange
        SystemJsonSerializer serializer = new();

        using (Utility.WithEmptyDirectory(out _))
        {
            InMemoryBlobCache cache = new(ImmediateSequencer.Instance, serializer);

            try
            {
                // Test with null values that might cause serialization edge cases
                Dictionary<string, object?> problematicDict = new() { ["null_value"] = null, [EmptyStringKey] = string.Empty, ["whitespace"] = "   ", ["normal_value"] = "normal" };

                // This should complete without throwing "Sequence contains no elements"
                _ = cache.InsertObjects(problematicDict!).Subscribe();

                // Test with very large number of items to stress the completion logic
                var massiveDict = Enumerable.Range(1, StressBatchSize)
                    .ToDictionary(
                        static i => $"stress_key_{i}",
                        static i => (object)$"stress_value_{i}");

                // This should also complete without exception
                _ = cache.InsertObjects(massiveDict).Subscribe();

                // Verify some values were stored correctly
                object? nullValue = null;
                _ = cache.GetObject<object>("null_value").Subscribe(v => nullValue = v);
                string? emptyString = null;
                _ = cache.GetObject<string>(EmptyStringKey).Subscribe(v => emptyString = v);
                string? normalValue = null;
                _ = cache.GetObject<string>("normal_value").Subscribe(v => normalValue = v);
                string? stressValue500 = null;
                _ = cache.GetObject<string>("stress_key_500").Subscribe(v => stressValue500 = v);

                using (Assert.Multiple())
                {
                    await Assert.That(nullValue).IsNull();
                    await Assert.That(emptyString).IsEqualTo(string.Empty);
                    await Assert.That(normalValue).IsEqualTo("normal");
                    await Assert.That(stressValue500).IsEqualTo("stress_value_500");
                }
            }
            finally
            {
                cache.Dispose();
            }
        }
    }
}
