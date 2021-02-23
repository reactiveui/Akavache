// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Akavache.Sqlite3;

using DiffEngine;

using PublicApiGenerator;

using Splat;

using Xunit;

namespace Akavache.APITests
{
    /// <summary>
    /// Tests for handling API approval.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ApiApprovalTests
    {
        private static readonly Regex _removeCoverletSectionRegex = new Regex(@"^namespace Coverlet\.Core\.Instrumentation\.Tracker.*?^}", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Tests to make sure the akavache project is approved.
        /// </summary>
        [Fact]
        public void AkavacheProject()
        {
            CheckApproval(typeof(SQLitePersistentBlobCache).Assembly);
        }

        /// <summary>
        /// Tests to make sure the akavache core project is approved.
        /// </summary>
        [Fact]
        public void AkavacheCore()
        {
            CheckApproval(typeof(BlobCache).Assembly);
        }

        /// <summary>
        /// Tests to make sure the akavache drawing project is approved.
        /// </summary>
#if !NETSTANDARD
        [Fact]
        public void AkavacheDrawing()
        {
            CheckApproval(typeof(Akavache.Drawing.Registrations).Assembly);
        }
#endif

        private static void CheckApproval(Assembly assembly, [CallerMemberName] string memberName = null, [CallerFilePath] string filePath = null)
        {
            var targetFrameworkName = Assembly.GetExecutingAssembly().GetTargetFrameworkName();

            var sourceDirectory = Path.GetDirectoryName(filePath);

            var approvedFileName = Path.Combine(sourceDirectory, $"ApiApprovalTests.{memberName}.{targetFrameworkName}.approved.txt");
            var receivedFileName = Path.Combine(sourceDirectory, $"ApiApprovalTests.{memberName}.{targetFrameworkName}.received.txt");

            if (!File.Exists(receivedFileName))
            {
                File.Create(receivedFileName).Close();
            }

            if (!File.Exists(approvedFileName))
            {
                File.Create(approvedFileName).Close();
            }

            var approvedPublicApi = File.ReadAllText(approvedFileName);

            var generatorOptions = new ApiGeneratorOptions { WhitelistedNamespacePrefixes = new[] { "Akavache" } };
            var receivedPublicApi = Filter(assembly.GeneratePublicApi(generatorOptions));

            if (!string.Equals(receivedPublicApi, approvedPublicApi, StringComparison.InvariantCulture))
            {
                File.WriteAllText(receivedFileName, receivedPublicApi);
                DiffRunner.Launch(receivedFileName, approvedFileName);
            }

            Assert.Equal(approvedPublicApi, receivedPublicApi);
        }

        private static string Filter(string text)
        {
            text = _removeCoverletSectionRegex.Replace(text, string.Empty);
            return string.Join(Environment.NewLine, text.Split(
                new[]
                {
                    Environment.NewLine
                },
                StringSplitOptions.RemoveEmptyEntries)
                    .Where(l =>
                    !l.StartsWith("[assembly: AssemblyVersion(", StringComparison.InvariantCulture) &&
                    !l.StartsWith("[assembly: AssemblyFileVersion(", StringComparison.InvariantCulture) &&
                    !l.StartsWith("[assembly: AssemblyInformationalVersion(", StringComparison.InvariantCulture) &&
                    !string.IsNullOrWhiteSpace(l)));
        }
    }
}
