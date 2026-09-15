using IncidentHub.Domain.Incidents;

namespace IncidentHub.Application.Incidents;

public sealed record IncidentDto(
    Guid Id,
    string Title,
    string Description,
    string Service,
    IncidentSeverity Severity,
    IncidentStatus Status,
    DateTime CreatedAt)
{
    public static IncidentDto FromIncident(Incident incident) =>
        new(
            incident.Id,
            incident.Title,
            incident.Description,
            incident.Service,
            incident.Severity,
            incident.Status,
            incident.CreatedAt);
}
