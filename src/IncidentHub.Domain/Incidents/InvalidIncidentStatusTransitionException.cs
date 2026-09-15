namespace IncidentHub.Domain.Incidents;

public sealed class InvalidIncidentStatusTransitionException(
    IncidentStatus currentStatus,
    IncidentStatus requestedStatus)
    : InvalidOperationException(
        $"Cannot change incident status from {currentStatus} to {requestedStatus}.")
{
}
