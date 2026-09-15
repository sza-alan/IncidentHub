using IncidentHub.Domain.Incidents;
using MediatR;

namespace IncidentHub.Application.Incidents.ChangeStatus;

public sealed record ChangeIncidentStatusCommand(
    Guid IncidentId,
    IncidentStatus NewStatus) : IRequest<IncidentDto?>;
