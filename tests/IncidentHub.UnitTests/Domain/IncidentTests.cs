using IncidentHub.Domain.Incidents;

namespace IncidentHub.UnitTests.Domain;

public sealed class IncidentTests
{
    [Fact]
    public void Constructor_WithEmptyTitle_ThrowsArgumentException()
    {
        var action = () => new Incident(
            "   ",
            "The API is unavailable.",
            "Payments API",
            IncidentSeverity.High);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_CreatesOpenIncident()
    {
        var incident = new Incident(
            "API unavailable",
            "The API is unavailable.",
            "Payments API",
            IncidentSeverity.High);

        Assert.Equal(IncidentStatus.Open, incident.Status);
    }
}
