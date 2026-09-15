using System.Text.Json;
using IncidentHub.Application.Services.Health;
using IncidentHub.Mcp.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace IncidentHub.IntegrationTests.Mcp;

public sealed class GetServiceHealthMcpToolTests
{
    [Fact]
    public async Task Call_HealthyService_ReturnsSanitizedHealth()
    {
        var checkedAt = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        var client = new StubServiceHealthClient((serviceName, _) =>
            Task.FromResult<ServiceHealthDto?>(new(
                serviceName,
                ServiceHealthStatus.Healthy,
                checkedAt)));
        var tool = CreateTool(client);

        var result = await tool.GetServiceHealthAsync(" Payments API ", default);

        Assert.True(result.Succeeded);
        Assert.Equal("Payments API", result.ServiceName);
        Assert.Equal("Healthy", result.Status);
        Assert.Equal(checkedAt, result.CheckedAt);
        Assert.Null(result.Error);
        Assert.Equal("Payments API", client.LastServiceName);
    }

    [Fact]
    public async Task Call_ServiceNotFound_ReturnsSanitizedError()
    {
        var tool = CreateTool(new StubServiceHealthClient((_, _) =>
            Task.FromResult<ServiceHealthDto?>(null)));

        var result = await tool.GetServiceHealthAsync("Payments API", default);

        Assert.False(result.Succeeded);
        Assert.Equal("service_not_found", result.Error);
    }

    [Theory]
    [InlineData(ServiceHealthFailureKind.Timeout, "health_timeout")]
    [InlineData(ServiceHealthFailureKind.Unavailable, "health_unavailable")]
    [InlineData(ServiceHealthFailureKind.InvalidResponse, "health_invalid_response")]
    public async Task Call_IntegrationFailure_ReturnsSanitizedError(
        ServiceHealthFailureKind failureKind,
        string expectedError)
    {
        var tool = CreateTool(new StubServiceHealthClient((_, _) =>
            throw new ServiceHealthIntegrationException(
                failureKind,
                "https://internal.example/health?token=super-secret")));

        var result = await tool.GetServiceHealthAsync("Payments API", default);
        var json = JsonSerializer.Serialize(result);

        Assert.False(result.Succeeded);
        Assert.Equal(expectedError, result.Error);
        Assert.DoesNotContain("internal.example", json);
        Assert.DoesNotContain("super-secret", json);
        Assert.DoesNotContain("stack", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Payments\nAPI")]
    public async Task Call_InvalidServiceName_DoesNotExecuteIntegration(string serviceName)
    {
        var client = new StubServiceHealthClient((_, _) =>
            throw new InvalidOperationException("The integration must not be called."));
        var tool = CreateTool(client);

        var result = await tool.GetServiceHealthAsync(serviceName, default);

        Assert.False(result.Succeeded);
        Assert.Equal("invalid_service_name", result.Error);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task Call_Cancellation_IsPropagatedToIntegration()
    {
        var client = new StubServiceHealthClient((_, cancellationToken) =>
        {
            Assert.True(cancellationToken.IsCancellationRequested);
            return Task.FromCanceled<ServiceHealthDto?>(cancellationToken);
        });
        var tool = CreateTool(client);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => tool.GetServiceHealthAsync("Payments API", cancellation.Token));
    }

    private static GetServiceHealthMcpTool CreateTool(IServiceHealthClient client) =>
        new(
            new ServiceHealthLookup(client),
            NullLogger<GetServiceHealthMcpTool>.Instance);

    private sealed class StubServiceHealthClient(
        Func<string, CancellationToken, Task<ServiceHealthDto?>> response)
        : IServiceHealthClient
    {
        public int CallCount { get; private set; }

        public string? LastServiceName { get; private set; }

        public Task<ServiceHealthDto?> GetHealthAsync(
            string serviceName,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastServiceName = serviceName;
            return response(serviceName, cancellationToken);
        }
    }
}
