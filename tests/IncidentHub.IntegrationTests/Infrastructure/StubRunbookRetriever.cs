using IncidentHub.Application.Incidents.Analyze;

public class StubRunbookRetriever : IRunbookRetriever
{
    public Task<string?> GetRunbookAsync(
        string serviceName,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult<string?>(null);
    }
}