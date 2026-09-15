using MediatR;

namespace IncidentHub.Application.Services.Health;

public sealed class GetServiceHealthQueryHandler(IServiceHealthClient serviceHealthClient)
    : IRequestHandler<GetServiceHealthQuery, ServiceHealthDto?>
{
    public Task<ServiceHealthDto?> Handle(
        GetServiceHealthQuery request,
        CancellationToken cancellationToken) =>
        serviceHealthClient.GetHealthAsync(request.ServiceName, cancellationToken);
}
