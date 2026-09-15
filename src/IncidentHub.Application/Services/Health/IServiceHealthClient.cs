namespace IncidentHub.Application.Services.Health;

public interface IServiceHealthClient
{
    Task<ServiceHealthDto?> GetHealthAsync(
        string serviceName,
        CancellationToken cancellationToken);
}
