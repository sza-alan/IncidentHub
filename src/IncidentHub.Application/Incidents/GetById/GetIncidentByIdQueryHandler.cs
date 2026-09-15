using MediatR;

namespace IncidentHub.Application.Incidents.GetById;

public sealed class GetIncidentByIdQueryHandler(IIncidentRepository incidentRepository)
    : IRequestHandler<GetIncidentByIdQuery, IncidentDto?>
{
    public async Task<IncidentDto?> Handle(
        GetIncidentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var incident = await incidentRepository.GetByIdAsync(request.Id, cancellationToken);

        return incident is null ? null : IncidentDto.FromIncident(incident);
    }
}
