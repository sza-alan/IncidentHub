using System.Net;
using System.Net.Http.Json;
using IncidentHub.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IncidentHub.IntegrationTests.Services;

public sealed class ServiceHealthEndpointTests(IncidentHubWebApplicationFactory factory)
    : IClassFixture<IncidentHubWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    [Fact]
    public async Task GetHealth_WhenExternalServiceSucceeds_ReturnsSimplifiedHealth()
    {
        factory.ServiceHealthHandler.Reset();
        factory.ServiceHealthHandler.EnqueueResponse(
            HttpStatusCode.OK,
            """{"service":"Payments API","status":"operational"}""");

        var response = await client.GetAsync("/api/services/Payments%20API/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var health = await response.Content.ReadFromJsonAsync<ServiceHealthResponse>();
        Assert.NotNull(health);
        Assert.Equal("Payments API", health.ServiceName);
        Assert.Equal("Healthy", health.Status);
        Assert.Equal(1, factory.ServiceHealthHandler.CallCount);
        Assert.EndsWith(
            "/services/Payments%20API/health",
            factory.ServiceHealthHandler.LastRequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task GetHealth_WhenTransientFailureRecovers_RetriesAndReturnsOk()
    {
        factory.ServiceHealthHandler.Reset();
        factory.ServiceHealthHandler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        factory.ServiceHealthHandler.EnqueueResponse(
            HttpStatusCode.OK,
            """{"service":"Payments API","status":"operational"}""");

        var response = await client.GetAsync("/api/services/Payments%20API/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, factory.ServiceHealthHandler.CallCount);
    }

    [Fact]
    public async Task GetHealth_WhenTransientFailuresAreExhausted_ReturnsServiceUnavailable()
    {
        factory.ServiceHealthHandler.Reset();
        factory.ServiceHealthHandler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        factory.ServiceHealthHandler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        factory.ServiceHealthHandler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);

        var response = await client.GetAsync("/api/services/Payments%20API/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(3, factory.ServiceHealthHandler.CallCount);
    }

    [Fact]
    public async Task GetHealth_WhenExternalServiceDoesNotKnowService_ReturnsNotFoundWithoutRetry()
    {
        factory.ServiceHealthHandler.Reset();
        factory.ServiceHealthHandler.EnqueueResponse(HttpStatusCode.NotFound);

        var response = await client.GetAsync("/api/services/Unknown/health");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(1, factory.ServiceHealthHandler.CallCount);
    }

    [Fact]
    public async Task GetHealth_WhenExternalResponseIsInvalid_ReturnsBadGatewayWithoutRetry()
    {
        factory.ServiceHealthHandler.Reset();
        factory.ServiceHealthHandler.EnqueueResponse(HttpStatusCode.OK, "not-json");

        var response = await client.GetAsync("/api/services/Payments%20API/health");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(1, factory.ServiceHealthHandler.CallCount);
    }

    [Fact]
    public async Task GetHealth_WhenAttemptsTimeOut_ReturnsGatewayTimeout()
    {
        factory.ServiceHealthHandler.Reset();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            factory.ServiceHealthHandler.Enqueue(async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
        }

        var response = await client.GetAsync("/api/services/Payments%20API/health");

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
        Assert.Equal(3, factory.ServiceHealthHandler.CallCount);
    }

    [Fact]
    public async Task GetHealth_WhenCallerCancels_DoesNotRetry()
    {
        factory.ServiceHealthHandler.Reset();
        var requestStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        factory.ServiceHealthHandler.Enqueue(async (_, cancellationToken) =>
        {
            requestStarted.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var cancellation = new CancellationTokenSource();
        var request = client.GetAsync(
            "/api/services/Payments%20API/health",
            cancellation.Token);

        await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        Assert.Equal(1, factory.ServiceHealthHandler.CallCount);
    }

    private sealed record ServiceHealthResponse(
        string ServiceName,
        string Status,
        DateTime CheckedAt);
}
