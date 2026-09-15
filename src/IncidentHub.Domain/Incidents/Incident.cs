namespace IncidentHub.Domain.Incidents;

public sealed class Incident
{
    private Incident()
    {
    }

    public Incident(
        string title,
        string? description,
        string service,
        IncidentSeverity severity)
    {
        if (!Enum.IsDefined(severity))
        {
            throw new ArgumentOutOfRangeException(nameof(severity));
        }

        Id = Guid.NewGuid();
        Title = RequireValue(title, nameof(title));
        Description = description?.Trim() ?? string.Empty;
        Service = RequireValue(service, nameof(service));
        Severity = severity;
        Status = IncidentStatus.Open;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string Service { get; private set; } = null!;

    public IncidentSeverity Severity { get; private set; }

    public IncidentStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public void ChangeStatus(IncidentStatus newStatus)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(newStatus));
        }

        if (Status == newStatus)
        {
            return;
        }

        var transitionIsAllowed = (Status, newStatus) switch
        {
            (IncidentStatus.Open, IncidentStatus.InProgress) => true,
            (IncidentStatus.Open, IncidentStatus.Resolved) => true,
            (IncidentStatus.InProgress, IncidentStatus.Resolved) => true,
            (IncidentStatus.Resolved, IncidentStatus.Open) => true,
            _ => false
        };

        if (!transitionIsAllowed)
        {
            throw new InvalidIncidentStatusTransitionException(Status, newStatus);
        }

        Status = newStatus;
    }

    private static string RequireValue(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", parameterName);
        }

        return value.Trim();
    }
}
