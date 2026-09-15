using IncidentHub.Domain.Incidents;
using MediatR;

namespace IncidentHub.Application.Incidents.Create;

public sealed record CreateIncidentCommand(
    string Title,
    string Description,
    string Service,
    IncidentSeverity Severity) : IRequest<IncidentDto>;
