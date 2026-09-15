using System.ComponentModel.DataAnnotations;
using IncidentHub.Domain.Incidents;

namespace IncidentHub.Api.Contracts.Incidents;

public sealed class ChangeIncidentStatusRequest
{
    [Required]
    public IncidentStatus? Status { get; init; }
}
