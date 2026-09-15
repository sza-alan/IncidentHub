namespace IncidentHub.Application.Services.Health;

public sealed record ServiceHealthDto(
    string ServiceName,
    ServiceHealthStatus Status,
    DateTime CheckedAt);
