namespace IncidentHub.Infrastructure.Integrations.ServiceHealth;

internal sealed record ExternalServiceHealthResponse(
    string Service,
    string Status);
