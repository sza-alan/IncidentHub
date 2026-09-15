using IncidentHub.Application.Incidents.Analyze;

namespace IncidentHub.Infrastructure.AI.Rag;

public sealed class FileRunbookRetriever : IRunbookRetriever
{
    private readonly string _runbooksPath;

    public FileRunbookRetriever(string runbooksPath)
    {
        _runbooksPath = runbooksPath;
    }

    public async Task<string?> GetRunbookAsync(
        string serviceName,
        CancellationToken cancellationToken)
    {
        var fileName = serviceName
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "-");

        var filePath = Path.Combine(
            _runbooksPath,
            $"{fileName}.md");

        if (!File.Exists(filePath))
        {
            return null;
        }

        return await File.ReadAllTextAsync(
            filePath,
            cancellationToken);
    }
}