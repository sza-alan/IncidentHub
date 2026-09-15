namespace IncidentHub.Application.Incidents.Analyze;

public interface IIncidentAnalyzer
{
    Task<IncidentAnalysisDto> AnalyzeAsync(
        IncidentAnalysisInput incident,
        CancellationToken cancellationToken);
}
