using IncidentHub.Domain.Incidents;

namespace IncidentHub.Application.Incidents.Analyze;

public sealed record IncidentAnalysisInput(
    Guid IncidentId,
    string Title,
    string Description,
    string Service,
    IncidentSeverity Severity,
    IncidentStatus Status,
    DateTime CreatedAt);
