using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using IncidentHub.Application.Services.Health;
using Microsoft.Extensions.Logging;
using Polly.Timeout;

namespace IncidentHub.Infrastructure.Integrations.ServiceHealth;

public sealed class ServiceHealthHttpClient(
    HttpClient httpClient,
    ILogger<ServiceHealthHttpClient> logger) : IServiceHealthClient
{
    public async Task<ServiceHealthDto?> GetHealthAsync(
        string serviceName,
        CancellationToken cancellationToken)
    {
        var requestUri = $"services/{Uri.EscapeDataString(serviceName)}/health";

        try
        {
            using var response = await httpClient.GetAsync(
                requestUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogDebug(
                    "Service {ServiceName} was not found by the external health service",
                    serviceName);

                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var failureKind = IsTransientStatusCode(response.StatusCode)
                    ? ServiceHealthFailureKind.Unavailable
                    : ServiceHealthFailureKind.InvalidResponse;

                logger.LogWarning(
                    "External health service failed for {ServiceName} with status code {StatusCode}",
                    serviceName,
                    (int)response.StatusCode);

                throw new ServiceHealthIntegrationException(
                    failureKind,
                    $"External health service returned HTTP {(int)response.StatusCode}.");
            }

            ExternalServiceHealthResponse? externalResponse;

            try
            {
                externalResponse = await response.Content
                    .ReadFromJsonAsync<ExternalServiceHealthResponse>(cancellationToken);
            }
            catch (JsonException exception)
            {
                logger.LogWarning(
                    exception,
                    "External health service returned invalid JSON for {ServiceName}",
                    serviceName);

                throw new ServiceHealthIntegrationException(
                    ServiceHealthFailureKind.InvalidResponse,
                    "External health service returned invalid JSON.",
                    exception);
            }

            if (externalResponse is null ||
                string.IsNullOrWhiteSpace(externalResponse.Service) ||
                string.IsNullOrWhiteSpace(externalResponse.Status))
            {
                logger.LogWarning(
                    "External health service returned an incomplete response for {ServiceName}",
                    serviceName);

                throw new ServiceHealthIntegrationException(
                    ServiceHealthFailureKind.InvalidResponse,
                    "External health service returned an incomplete response.");
            }

            var status = MapStatus(externalResponse.Status, serviceName);

            logger.LogDebug(
                "External health check completed for {ServiceName} with status {HealthStatus}",
                serviceName,
                status);

            return new ServiceHealthDto(
                externalResponse.Service,
                status,
                DateTime.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TimeoutRejectedException exception)
        {
            logger.LogWarning(
                exception,
                "External health check timed out for {ServiceName}",
                serviceName);

            throw new ServiceHealthIntegrationException(
                ServiceHealthFailureKind.Timeout,
                "External health service timed out.",
                exception);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "External health service is unavailable for {ServiceName}",
                serviceName);

            throw new ServiceHealthIntegrationException(
                ServiceHealthFailureKind.Unavailable,
                "External health service is unavailable.",
                exception);
        }
    }

    internal static bool IsTransientStatusCode(HttpStatusCode statusCode) =>
        statusCode is
            HttpStatusCode.RequestTimeout or
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout;

    private ServiceHealthStatus MapStatus(string externalStatus, string serviceName)
    {
        if (externalStatus.Equals("operational", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceHealthStatus.Healthy;
        }

        if (externalStatus.Equals("degraded", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceHealthStatus.Degraded;
        }

        if (externalStatus.Equals("outage", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceHealthStatus.Unhealthy;
        }

        logger.LogWarning(
            "External health service returned unknown status {ExternalStatus} for {ServiceName}",
            externalStatus,
            serviceName);

        throw new ServiceHealthIntegrationException(
            ServiceHealthFailureKind.InvalidResponse,
            "External health service returned an unknown status.");
    }
}
