﻿// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Disposables;

namespace Akavache.Tests;

/// <summary>
/// A set of utility helper methods for use throughout tests.
/// </summary>
internal static class Utility
{
    /// <summary>
    /// Deletes a directory.
    /// </summary>
    /// <param name="directoryPath">The path to delete.</param>
    public static void DeleteDirectory(string directoryPath)
    {
        // From http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502
        try
        {
            var di = new DirectoryInfo(directoryPath);
            var files = di.EnumerateFiles();
            var dirs = di.EnumerateDirectories();

            foreach (var file in files)
            {
                File.SetAttributes(file.FullName, FileAttributes.Normal);
                new Action(() => file.Delete()).Retry();
            }

            foreach (var dir in dirs)
            {
                DeleteDirectory(dir.FullName);
            }

            File.SetAttributes(directoryPath, FileAttributes.Normal);
            Directory.Delete(directoryPath, false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("***** Failed to clean up!! *****");
            Console.Error.WriteLine(ex);
        }
    }

    public static IDisposable WithEmptyDirectory(out string directoryPath)
    {
        var di = new DirectoryInfo(Path.Combine(".", Guid.NewGuid().ToString()));
        if (di.Exists)
        {
            DeleteDirectory(di.FullName);
        }

        di.Create();

        directoryPath = di.FullName;
        return Disposable.Create(() => DeleteDirectory(di.FullName));
    }

    public static void Retry(this Action block, int retries = 2)
    {
        while (true)
        {
            try
            {
                block();
                return;
            }
            catch (Exception) when (retries != 0)
            {
                retries--;
                Thread.Sleep(10);
            }
        }
    }
}
