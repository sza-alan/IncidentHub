using IncidentHub.Application.Common;
using IncidentHub.Domain.Incidents;

namespace IncidentHub.Application.Incidents;

public interface IIncidentRepository
{
    Task AddAsync(Incident incident, CancellationToken cancellationToken);

    Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<Incident>> ListAsync(
        int page,
        int pageSize,
        IncidentStatus? status,
        IncidentSeverity? severity,
        string? service,
        CancellationToken cancellationToken);

    Task<Incident?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken);

    Task UpdateAsync(Incident incident, CancellationToken cancellationToken);
}
