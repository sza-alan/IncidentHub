namespace IncidentHub.Application.Services.Health;

public sealed class ServiceHealthLookup(IServiceHealthClient serviceHealthClient)
{
    public async Task<ServiceHealthLookupResult> GetAsync(
        string serviceName,
        CancellationToken cancellationToken)
    {
        try
        {
            var health = await serviceHealthClient.GetHealthAsync(
                serviceName,
                cancellationToken);

            return health is null
                ? new ServiceHealthLookupResult(
                    false,
                    serviceName,
                    null,
                    null,
                    "service_not_found")
                : new ServiceHealthLookupResult(
                    true,
                    health.ServiceName,
                    health.Status.ToString(),
                    health.CheckedAt,
                    null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ServiceHealthIntegrationException exception)
        {
            var error = exception.FailureKind switch
            {
                ServiceHealthFailureKind.Timeout => "health_timeout",
                ServiceHealthFailureKind.Unavailable => "health_unavailable",
                ServiceHealthFailureKind.InvalidResponse => "health_invalid_response",
                _ => "health_unavailable"
            };

            return new ServiceHealthLookupResult(
                false,
                serviceName,
                null,
                null,
                error);
        }
    }
}
