using MediatR;

namespace IncidentHub.Application.Incidents.Analyze;

public sealed class AnalyzeIncidentQueryHandler(
    IIncidentRepository incidentRepository,
    IIncidentAnalyzer incidentAnalyzer)
    : IRequestHandler<AnalyzeIncidentQuery, IncidentAnalysisDto?>
{
    public async Task<IncidentAnalysisDto?> Handle(
        AnalyzeIncidentQuery request,
        CancellationToken cancellationToken)
    {
        var incident = await incidentRepository.GetByIdAsync(
            request.IncidentId,
            cancellationToken);

        if (incident is null)
        {
            return null;
        }

        var input = new IncidentAnalysisInput(
            incident.Id,
            incident.Title,
            incident.Description,
            incident.Service,
            incident.Severity,
            incident.Status,
            incident.CreatedAt);

        return await incidentAnalyzer.AnalyzeAsync(input, cancellationToken);
    }
}
