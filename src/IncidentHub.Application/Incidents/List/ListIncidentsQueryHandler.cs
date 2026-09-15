using IncidentHub.Application.Common;
using MediatR;

namespace IncidentHub.Application.Incidents.List;

public sealed class ListIncidentsQueryHandler(IIncidentRepository incidentRepository)
    : IRequestHandler<ListIncidentsQuery, PagedResult<IncidentDto>>
{
    public async Task<PagedResult<IncidentDto>> Handle(
        ListIncidentsQuery request,
        CancellationToken cancellationToken)
    {
        var incidents = await incidentRepository.ListAsync(
            request.Page,
            request.PageSize,
            request.Status,
            request.Severity,
            request.Service,
            cancellationToken);

        var items = incidents.Items
            .Select(IncidentDto.FromIncident)
            .ToList();

        return new PagedResult<IncidentDto>(
            items,
            incidents.Page,
            incidents.PageSize,
            incidents.TotalCount);
    }
}
