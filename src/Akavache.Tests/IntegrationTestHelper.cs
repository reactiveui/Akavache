using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;

namespace Akavache.Tests
{
    public static class IntegrationTestHelper
    {
        public static string GetPath(params string[] paths)
        {
            var ret = GetIntegrationTestRootDirectory();
            return (new FileInfo(paths.Aggregate(ret, Path.Combine))).FullName;
        }

        public static string GetIntegrationTestRootDirectory()
        {
            // XXX: This is an evil hack, but it's okay for a unit test
            // We can't use Assembly.Location because unit test runners love
            // to move stuff to temp directories
            var st = new StackFrame(true);
            var di = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(st.GetFileName())));

            return di.FullName;
        }

        public static HttpResponseMessage GetResponse(params string[] paths)
        {
            var bytes = File.ReadAllBytes(GetPath(paths));

            // Find the body
            var bodyIndex = -1;
            for (bodyIndex = 0; bodyIndex < bytes.Length - 3; bodyIndex++)
            {
                if (bytes[bodyIndex] != 0x0D || bytes[bodyIndex + 1] != 0x0A ||
                    bytes[bodyIndex + 2] != 0x0D || bytes[bodyIndex + 3] != 0x0A)
                {
                    continue;
                }

                goto foundIt;
            }

            throw new Exception("Couldn't find response body");

        foundIt:

            var headerText = Encoding.UTF8.GetString(bytes, 0, bodyIndex);
            var lines = headerText.Split('\n');
            var statusCode = (HttpStatusCode)Int32.Parse(lines[0].Split(' ')[1]);
            var ret = new HttpResponseMessage(statusCode);

            ret.Content = new ByteArrayContent(bytes, bodyIndex + 2, bytes.Length - bodyIndex - 2);

            foreach (var line in lines.Skip(1))
            {
                var separatorIndex = line.IndexOf(':');
                var key = line.Substring(0, separatorIndex);
                var val = line.Substring(separatorIndex + 2).TrimEnd();

                if (String.IsNullOrWhiteSpace(line)) continue;

                ret.Headers.TryAddWithoutValidation(key, val);
                ret.Content.Headers.TryAddWithoutValidation(key, val);
            }

            return ret;
        }
    }
}