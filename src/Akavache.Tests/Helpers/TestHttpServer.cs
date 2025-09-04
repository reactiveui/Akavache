// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;

namespace Akavache.Tests.Helpers;

/// <summary>
/// A simple local HTTP server for testing HTTP functionality without external dependencies.
/// Uses only built-in .NET types, no external packages required.
/// </summary>
public sealed class TestHttpServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Dictionary<string, TestResponse> _responses;
    private readonly Task _serverTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestHttpServer"/> class.
    /// </summary>
    public TestHttpServer()
    {
        _listener = new HttpListener();
        _cancellationTokenSource = new CancellationTokenSource();
        _responses = [];

        // Try to find an available port starting from 8999
        BaseUrl = FindAvailablePort();
        _listener.Prefixes.Add(BaseUrl);
        _listener.Start();

        _serverTask = Task.Run(async () => await ProcessRequestsAsync(_cancellationTokenSource.Token));
    }

    /// <summary>
    /// Gets the base URL of the test server.
    /// </summary>
    public string BaseUrl { get; }

    /// <summary>
    /// Sets up a response for a specific path.
    /// </summary>
    /// <param name="path">The path to respond to (e.g., "/html", "/json").</param>
    /// <param name="content">The content to return.</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="contentType">The content type header.</param>
    public void SetupResponse(string path, string content, HttpStatusCode statusCode = HttpStatusCode.OK, string contentType = "text/html") => _responses[path] = new TestResponse(content, statusCode, contentType);

    /// <summary>
    /// Sets up default responses that mimic httpbin.org behavior.
    /// </summary>
    public void SetupDefaultResponses()
    {
        SetupResponse("/html", "<html><head><title>Test HTML</title></head><body><h1>Test Content</h1></body></html>", HttpStatusCode.OK, "text/html");
        SetupResponse("/json", "{\"key\": \"value\", \"test\": true}", HttpStatusCode.OK, "application/json");
        SetupResponse("/user-agent", "{\"user-agent\": \"test-client\"}", HttpStatusCode.OK, "application/json");
        SetupResponse("/status/200", "OK", HttpStatusCode.OK, "text/plain");
        SetupResponse("/status/404", "Not Found", HttpStatusCode.NotFound, "text/plain");
        SetupResponse("/status/500", "Internal Server Error", HttpStatusCode.InternalServerError, "text/plain");
    }

    /// <summary>
    /// Disposes the test server.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        _listener.Stop();
        _listener.Close();

        try
        {
            _serverTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (TaskCanceledException)
        {
            // Expected when cancelling
        }
        catch (AggregateException)
        {
            // Expected when cancelling
        }

        _cancellationTokenSource.Dispose();
        _disposed = true;
    }

    private static string FindAvailablePort()
    {
        for (var port = 8999; port <= 9099; port++)
        {
            try
            {
                var testListener = new HttpListener();
                var url = $"http://localhost:{port}/";
                testListener.Prefixes.Add(url);
                testListener.Start();
                testListener.Stop();
                testListener.Close();
                return url;
            }
            catch (Exception)
            {
                // Port is not available, try next one
                continue;
            }
        }

        // Fallback to original port if nothing else works
        return "http://localhost:8999/";
    }

    private async Task ProcessRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => ProcessRequest(context), cancellationToken);
            }
            catch (ObjectDisposedException)
            {
                // Server is shutting down
                break;
            }
            catch (HttpListenerException)
            {
                // Server is shutting down
                break;
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1075:Avoid empty catch clause that catches System.Exception", Justification = "Deliberate")]
    private void ProcessRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;
            var path = request.Url?.AbsolutePath ?? "/";

            if (_responses.TryGetValue(path, out var testResponse))
            {
                response.StatusCode = (int)testResponse.StatusCode;
                response.ContentType = testResponse.ContentType;

                var buffer = Encoding.UTF8.GetBytes(testResponse.Content);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else
            {
                response.StatusCode = 404;
                response.ContentType = "text/plain";
                var buffer = Encoding.UTF8.GetBytes("Not Found");
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }

            response.OutputStream.Close();
        }
        catch (Exception)
        {
            // Ignore errors during request processing
        }
    }

    private record TestResponse(string Content, HttpStatusCode StatusCode, string ContentType);
}
