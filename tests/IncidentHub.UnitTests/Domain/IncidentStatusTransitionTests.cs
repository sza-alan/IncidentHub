using IncidentHub.Domain.Incidents;

namespace IncidentHub.UnitTests.Domain;

public sealed class IncidentStatusTransitionTests
{
    [Fact]
    public void ChangeStatus_FromOpenToInProgress_ChangesStatus()
    {
        var incident = CreateIncident();

        incident.ChangeStatus(IncidentStatus.InProgress);

        Assert.Equal(IncidentStatus.InProgress, incident.Status);
    }

    [Fact]
    public void ChangeStatus_FromOpenToResolved_ChangesStatus()
    {
        var incident = CreateIncident();

        incident.ChangeStatus(IncidentStatus.Resolved);

        Assert.Equal(IncidentStatus.Resolved, incident.Status);
    }

    [Fact]
    public void ChangeStatus_FromInProgressToResolved_ChangesStatus()
    {
        var incident = CreateIncident();
        incident.ChangeStatus(IncidentStatus.InProgress);

        incident.ChangeStatus(IncidentStatus.Resolved);

        Assert.Equal(IncidentStatus.Resolved, incident.Status);
    }

    [Fact]
    public void ChangeStatus_FromResolvedToOpen_ReopensIncident()
    {
        var incident = CreateIncident();
        incident.ChangeStatus(IncidentStatus.Resolved);

        incident.ChangeStatus(IncidentStatus.Open);

        Assert.Equal(IncidentStatus.Open, incident.Status);
    }

    [Fact]
    public void ChangeStatus_ToCurrentStatus_IsIdempotent()
    {
        var incident = CreateIncident();

        incident.ChangeStatus(IncidentStatus.Open);

        Assert.Equal(IncidentStatus.Open, incident.Status);
    }

    [Fact]
    public void ChangeStatus_FromInProgressToOpen_ThrowsInvalidTransition()
    {
        var incident = CreateIncident();
        incident.ChangeStatus(IncidentStatus.InProgress);

        var action = () => incident.ChangeStatus(IncidentStatus.Open);

        Assert.Throws<InvalidIncidentStatusTransitionException>(action);
    }

    [Fact]
    public void ChangeStatus_FromResolvedToInProgress_ThrowsInvalidTransition()
    {
        var incident = CreateIncident();
        incident.ChangeStatus(IncidentStatus.Resolved);

        var action = () => incident.ChangeStatus(IncidentStatus.InProgress);

        Assert.Throws<InvalidIncidentStatusTransitionException>(action);
    }

    private static Incident CreateIncident() =>
        new(
            "API unavailable",
            "The API is unavailable.",
            "Payments API",
            IncidentSeverity.High);
}
