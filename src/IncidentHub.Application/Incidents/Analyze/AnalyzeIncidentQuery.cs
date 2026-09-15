using MediatR;

namespace IncidentHub.Application.Incidents.Analyze;

public sealed record AnalyzeIncidentQuery(Guid IncidentId)
    : IRequest<IncidentAnalysisDto?>;
