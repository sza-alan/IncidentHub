using IncidentHub.Application.Incidents;
using IncidentHub.Application.Common;
using IncidentHub.Domain.Incidents;
using IncidentHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IncidentHub.Infrastructure.Repositories;

public sealed class IncidentRepository(IncidentHubDbContext dbContext) : IIncidentRepository
{
    public async Task AddAsync(Incident incident, CancellationToken cancellationToken)
    {
        await dbContext.Incidents.AddAsync(incident, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Incident?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Incidents
            .AsNoTracking()
            .SingleOrDefaultAsync(incident => incident.Id == id, cancellationToken);

    public async Task<PagedResult<Incident>> ListAsync(
        int page,
        int pageSize,
        IncidentStatus? status,
        IncidentSeverity? severity,
        string? service,
        CancellationToken cancellationToken)
    {
        IQueryable<Incident> query = dbContext.Incidents.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(incident => incident.Status == status.Value);
        }

        if (severity.HasValue)
        {
            query = query.Where(incident => incident.Severity == severity.Value);
        }

        if (!string.IsNullOrWhiteSpace(service))
        {
            var normalizedService = service.Trim();
            query = query.Where(incident => incident.Service == normalizedService);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(incident => incident.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Incident>(items, page, pageSize, totalCount);
    }

    public Task<Incident?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Incidents.SingleOrDefaultAsync(
            incident => incident.Id == id,
            cancellationToken);

    public async Task UpdateAsync(Incident incident, CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
