using MediatR;

namespace IncidentHub.Application.Services.Health;

public sealed record GetServiceHealthQuery(string ServiceName)
    : IRequest<ServiceHealthDto?>;
