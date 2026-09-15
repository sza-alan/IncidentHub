using System.ComponentModel.DataAnnotations;
using IncidentHub.Domain.Incidents;

namespace IncidentHub.Api.Contracts.Incidents;

public sealed class ListIncidentsRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public IncidentStatus? Status { get; init; }

    public IncidentSeverity? Severity { get; init; }

    public string? Service { get; init; }
}
