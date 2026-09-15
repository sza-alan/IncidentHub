using IncidentHub.Application.Common;
using IncidentHub.Domain.Incidents;
using MediatR;

namespace IncidentHub.Application.Incidents.List;

public sealed record ListIncidentsQuery(
    int Page,
    int PageSize,
    IncidentStatus? Status,
    IncidentSeverity? Severity,
    string? Service) : IRequest<PagedResult<IncidentDto>>;
