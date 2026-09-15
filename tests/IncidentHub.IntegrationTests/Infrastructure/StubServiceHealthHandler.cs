using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace IncidentHub.IntegrationTests.Infrastructure;

public sealed class StubServiceHealthHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> responses = new();
    private int callCount;

    public int CallCount => Volatile.Read(ref callCount);

    public Uri? LastRequestUri { get; private set; }

    public void Reset()
    {
        responses.Clear();
        Interlocked.Exchange(ref callCount, 0);
        LastRequestUri = null;
    }

    public void EnqueueResponse(HttpStatusCode statusCode, string? json = null)
    {
        Enqueue((_, _) => Task.FromResult(CreateResponse(statusCode, json)));
    }

    public void Enqueue(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response)
    {
        responses.Enqueue(response);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref callCount);
        LastRequestUri = request.RequestUri;

        if (!responses.TryDequeue(out var response))
        {
            return Task.FromResult(CreateResponse(HttpStatusCode.InternalServerError));
        }

        return response(request, cancellationToken);
    }

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string? json = null)
    {
        var response = new HttpResponseMessage(statusCode);

        if (json is not null)
        {
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return response;
    }
}
