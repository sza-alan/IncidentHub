namespace IncidentHub.Application.Services.Health;

public sealed record ServiceHealthLookupResult(
    bool Succeeded,
    string ServiceName,
    string? Status,
    DateTime? CheckedAt,
    string? Error);
