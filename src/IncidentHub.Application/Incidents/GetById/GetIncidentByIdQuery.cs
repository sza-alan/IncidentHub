using MediatR;

namespace IncidentHub.Application.Incidents.GetById;

public sealed record GetIncidentByIdQuery(Guid Id) : IRequest<IncidentDto?>;
