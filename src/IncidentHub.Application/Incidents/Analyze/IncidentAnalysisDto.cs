namespace IncidentHub.Application.Incidents.Analyze;

public sealed record IncidentAnalysisDto(
    Guid IncidentId,
    string Summary,
    IReadOnlyList<string> Facts,
    IReadOnlyList<string> Hypotheses,
    IReadOnlyList<string> RecommendedActions,
    double Confidence);
