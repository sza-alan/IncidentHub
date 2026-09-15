using IncidentHub.Application.Incidents.Analyze;

namespace IncidentHub.IntegrationTests.Infrastructure;

public sealed class StubIncidentAnalyzer : IIncidentAnalyzer
{
    public IncidentAnalysisFailureKind? FailureKind { get; set; }

    public IncidentAnalysisInput? LastInput { get; private set; }

    public bool ReceivedCancelableToken { get; private set; }

    public Task<IncidentAnalysisDto> AnalyzeAsync(
        IncidentAnalysisInput incident,
        CancellationToken cancellationToken)
    {
        LastInput = incident;
        ReceivedCancelableToken = cancellationToken.CanBeCanceled;

        if (FailureKind is { } failureKind)
        {
            throw new IncidentAnalysisException(failureKind, "Simulated AI failure.");
        }

        return Task.FromResult(new IncidentAnalysisDto(
            incident.IncidentId,
            "The payments API is unavailable.",
            ["The incident is open.", "Severity is High."],
            ["A downstream dependency may be unavailable."],
            ["Inspect recent application logs."],
            0.72));
    }

    public void Reset()
    {
        FailureKind = null;
        LastInput = null;
        ReceivedCancelableToken = false;
    }
}
