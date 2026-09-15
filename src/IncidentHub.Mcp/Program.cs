using IncidentHub.Application.Services.Health;
using IncidentHub.Infrastructure.Integrations.ServiceHealth;
using IncidentHub.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
    options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddScoped<ServiceHealthLookup>();
builder.Services.AddServiceHealthIntegration(builder.Configuration);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<GetServiceHealthMcpTool>();

await builder.Build().RunAsync();
