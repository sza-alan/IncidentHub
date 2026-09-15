using System.ComponentModel.DataAnnotations;
using IncidentHub.Domain.Incidents;

namespace IncidentHub.Api.Contracts.Incidents;

public sealed record CreateIncidentRequest(
    [param: Required] string Title,
    string? Description,
    [param: Required] string Service,
    [param: Required] IncidentSeverity? Severity);
