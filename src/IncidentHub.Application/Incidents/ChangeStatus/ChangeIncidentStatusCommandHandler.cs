using MediatR;

namespace IncidentHub.Application.Incidents.ChangeStatus;

public sealed class ChangeIncidentStatusCommandHandler(IIncidentRepository incidentRepository)
    : IRequestHandler<ChangeIncidentStatusCommand, IncidentDto?>
{
    public async Task<IncidentDto?> Handle(
        ChangeIncidentStatusCommand request,
        CancellationToken cancellationToken)
    {
        var incident = await incidentRepository.GetByIdForUpdateAsync(
            request.IncidentId,
            cancellationToken);

        if (incident is null)
        {
            return null;
        }

        incident.ChangeStatus(request.NewStatus);

        await incidentRepository.UpdateAsync(incident, cancellationToken);

        return IncidentDto.FromIncident(incident);
    }
}
