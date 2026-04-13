// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Akavache.Tests.Helpers;

/// <summary>
/// A simple local HTTP server for testing HTTP functionality without external
/// dependencies. Built on <see cref="TcpListener"/> rather than
/// <see cref="HttpListener"/> so that shutdown races in the managed HttpListener
/// stack can't crash the test host — on .NET 8 Linux, HttpListener's read
/// completion callbacks race with <c>Stop</c>/<c>Close</c> and raise a
/// <see cref="NullReferenceException"/> from <c>HttpConnection.get_LocalEndPoint</c>
/// on a ThreadPool worker, which is unhandled and terminates the process.
/// </summary>
public sealed class TestHttpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Dictionary<string, TestResponse> _responses;
    private readonly Task _serverTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestHttpServer"/> class,
    /// binding to an ephemeral port on the loopback interface.
    /// </summary>
    public TestHttpServer()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _responses = [];

        _listener = new TcpListener(IPAddress.Loopback, port: 0);
        _listener.Start();

        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        BaseUrl = $"http://localhost:{port}/";

        _serverTask = Task.Run(() => AcceptLoopAsync(_cancellationTokenSource.Token));
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
    public void SetupResponse(string path, string content, HttpStatusCode statusCode = HttpStatusCode.OK, string contentType = "text/html") =>
        _responses[path] = new TestResponse(content, statusCode, contentType);

    /// <summary>
    /// Sets up default responses that mimic httpbin.org behaviour.
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
    /// Disposes the test server, stopping the listener and draining the accept
    /// loop.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _cancellationTokenSource.Cancel();

        try
        {
            _listener.Stop();
        }
        catch
        {
            // Best-effort: listener may already be in a tearing-down state.
        }

        (_listener as IDisposable)?.Dispose();

        try
        {
            _serverTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Expected on cancellation.
        }

        _cancellationTokenSource.Dispose();
    }

    private static async Task<string?> ReadRequestPathAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;

            var headerEnd = IndexOfDoubleCrlf(buffer, total);
            if (headerEnd < 0)
            {
                continue;
            }

            var request = Encoding.ASCII.GetString(buffer, 0, headerEnd);
            var firstLineEnd = request.IndexOf('\r');
            if (firstLineEnd < 0)
            {
                return null;
            }

            var firstLine = request.Substring(0, firstLineEnd);
            var parts = firstLine.Split(' ');
            if (parts.Length < 2)
            {
                return null;
            }

            var target = parts[1];
            var queryStart = target.IndexOf('?');
            return queryStart >= 0 ? target.Substring(0, queryStart) : target;
        }

        return null;
    }

    private static int IndexOfDoubleCrlf(byte[] buffer, int length)
    {
        for (var i = 0; i + 3 < length; i++)
        {
            if (buffer[i] == (byte)'\r' && buffer[i + 1] == (byte)'\n' && buffer[i + 2] == (byte)'\r' && buffer[i + 3] == (byte)'\n')
            {
                return i;
            }
        }

        return -1;
    }

    private static async Task WriteResponseAsync(NetworkStream stream, TestResponse response, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(response.Content);
        var statusCode = (int)response.StatusCode;
        var reasonPhrase = response.StatusCode.ToString();
        var headers =
            $"HTTP/1.1 {statusCode} {reasonPhrase}\r\n" +
            $"Content-Type: {response.ContentType}; charset=utf-8\r\n" +
            $"Content-Length: {body.Length}\r\n" +
            "Connection: close\r\n" +
            "\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(headers);

        await stream.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }

            _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using (client)
            {
                client.NoDelay = true;
                using var stream = client.GetStream();
                stream.ReadTimeout = 5_000;
                stream.WriteTimeout = 5_000;

                var path = await ReadRequestPathAsync(stream, cancellationToken).ConfigureAwait(false);
                if (path is null)
                {
                    return;
                }

                var response = _responses.TryGetValue(path, out var configured)
                    ? configured
                    : new TestResponse("Not Found", HttpStatusCode.NotFound, "text/plain");

                await WriteResponseAsync(stream, response, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            // Tests don't care about per-connection failures; the unit under test
            // will surface the resulting HTTP error.
        }
    }

    private sealed record TestResponse(string Content, HttpStatusCode StatusCode, string ContentType);
}
