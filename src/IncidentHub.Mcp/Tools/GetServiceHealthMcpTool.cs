using System.ComponentModel;
using IncidentHub.Application.Services.Health;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace IncidentHub.Mcp.Tools;

[McpServerToolType]
public sealed class GetServiceHealthMcpTool(
    ServiceHealthLookup serviceHealthLookup,
    ILogger<GetServiceHealthMcpTool> logger)
{
    public const string ToolName = "get_service_health";
    private const int MaximumServiceNameLength = 200;

    [McpServerTool(Name = ToolName, UseStructuredContent = true)]
    [Description("Consulta o health atual de um serviço e retorna somente dados sanitizados.")]
    public async Task<ServiceHealthLookupResult> GetServiceHealthAsync(
        [Description("Nome do serviço a consultar.")] string serviceName,
        CancellationToken cancellationToken)
    {
        var normalizedServiceName = serviceName?.Trim();
        if (string.IsNullOrEmpty(normalizedServiceName)
            || normalizedServiceName.Length > MaximumServiceNameLength
            || normalizedServiceName.Any(char.IsControl))
        {
            logger.LogWarning(
                "Rejected MCP tool call. ToolName: {ToolName}; Outcome: {Outcome}",
                ToolName,
                "invalid_service_name");

            return new ServiceHealthLookupResult(
                false,
                string.Empty,
                null,
                null,
                "invalid_service_name");
        }

        var result = await serviceHealthLookup.GetAsync(
            normalizedServiceName,
            cancellationToken);

        logger.Log(
            result.Succeeded ? LogLevel.Information : LogLevel.Warning,
            "Executed MCP tool. ToolName: {ToolName}; ServiceName: {ServiceName}; Outcome: {Outcome}",
            ToolName,
            normalizedServiceName,
            result.Error ?? "success");

        return result;
    }
}
