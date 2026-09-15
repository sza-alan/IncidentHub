using System.ComponentModel;
using System.Diagnostics;
using IncidentHub.Application.Incidents.Analyze;
using IncidentHub.Application.Services.Health;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace IncidentHub.Infrastructure.AI.Tools;

internal sealed class GetServiceHealthTool(
    Guid incidentId,
    string allowedServiceName,
    ServiceHealthLookup serviceHealthLookup,
    ILogger logger)
{
    internal const string Name = "get_service_health";
    internal const int MaximumCalls = 2;

    private int callCount;
    private readonly List<ServiceHealthLookupResult> results = [];

    public IReadOnlyList<ServiceHealthLookupResult> Results => results.ToArray();

    public AIFunction CreateFunction() => AIFunctionFactory.Create(
        ExecuteAsync,
        Name,
        "Consulta o status atual de saúde do serviço associado ao incidente. " +
        "Use somente o nome exato do serviço informado no incidente.");

    private async Task<ServiceHealthLookupResult> ExecuteAsync(
        [Description("Nome exato do serviço associado ao incidente.")] string serviceName,
        CancellationToken cancellationToken)
    {
        var toolCallNumber = Interlocked.Increment(ref callCount);
        if (toolCallNumber > MaximumCalls)
        {
            logger.LogWarning(
                "Incident analysis tool call limit exceeded. IncidentId: {IncidentId}; ToolName: {ToolName}; ToolCallNumber: {ToolCallNumber}",
                incidentId,
                Name,
                toolCallNumber);

            throw new IncidentAnalysisException(
                IncidentAnalysisFailureKind.InvalidResponse,
                $"The model exceeded the limit of {MaximumCalls} tool calls per analysis.");
        }

        var requestedService = serviceName?.Trim();
        if (string.IsNullOrEmpty(requestedService)
            || requestedService.Length > 200
            || !string.Equals(
                requestedService,
                allowedServiceName,
                StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Rejected incident analysis tool call. IncidentId: {IncidentId}; ToolName: {ToolName}; RequestedService: {RequestedService}; ToolCallNumber: {ToolCallNumber}; Outcome: {Outcome}",
                incidentId,
                Name,
                requestedService,
                toolCallNumber,
                "service_not_allowed");

            return Record(new ServiceHealthLookupResult(
                false,
                allowedServiceName,
                null,
                null,
                "service_not_allowed"));
        }

        var stopwatch = Stopwatch.StartNew();

        var result = await serviceHealthLookup.GetAsync(
            allowedServiceName,
            cancellationToken);
        stopwatch.Stop();

        logger.Log(
            result.Succeeded ? LogLevel.Information : LogLevel.Warning,
            "Executed incident analysis tool. IncidentId: {IncidentId}; ToolName: {ToolName}; RequestedService: {RequestedService}; ToolCallNumber: {ToolCallNumber}; Outcome: {Outcome}; DurationMilliseconds: {DurationMilliseconds}",
            incidentId,
            Name,
            requestedService,
            toolCallNumber,
            result.Error ?? "success",
            stopwatch.ElapsedMilliseconds);

        return Record(result);
    }

    private ServiceHealthLookupResult Record(ServiceHealthLookupResult result)
    {
        results.Add(result);
        return result;
    }
}
