using IncidentHub.Domain.Incidents;
using MediatR;

namespace IncidentHub.Application.Incidents.Create;

public sealed class CreateIncidentCommandHandler(IIncidentRepository incidentRepository)
    : IRequestHandler<CreateIncidentCommand, IncidentDto>
{
    public async Task<IncidentDto> Handle(
        CreateIncidentCommand request,
        CancellationToken cancellationToken)
    {
        var incident = new Incident(
            request.Title,
            request.Description,
            request.Service,
            request.Severity);

        await incidentRepository.AddAsync(incident, cancellationToken);

        return IncidentDto.FromIncident(incident);
    }
}
