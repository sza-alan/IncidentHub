namespace IncidentHub.Application.Incidents.Analyze
{
    public interface IRunbookRetriever
    {
        Task<string?> GetRunbookAsync(
            string serviceName,
            CancellationToken cancellationToken);
    }
}
