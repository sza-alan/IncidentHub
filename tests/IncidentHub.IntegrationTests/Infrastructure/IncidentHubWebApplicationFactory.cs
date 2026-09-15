using IncidentHub.Application.Services.Health;
using IncidentHub.Application.Incidents.Analyze;
using IncidentHub.Infrastructure.Integrations.ServiceHealth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IncidentHub.IntegrationTests.Infrastructure;

public sealed class IncidentHubWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databasePath = Path.Combine(
        Path.GetTempPath(),
        $"incidenthub-tests-{Guid.NewGuid():N}.db");

    public StubServiceHealthHandler ServiceHealthHandler { get; } = new();

    public StubIncidentAnalyzer IncidentAnalyzer { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    $"Data Source={databasePath};Pooling=False",
                ["ExternalServices:ServiceHealth:AttemptTimeoutMilliseconds"] = "100",
                ["ExternalServices:ServiceHealth:TotalTimeoutMilliseconds"] = "500",
                ["ExternalServices:ServiceHealth:RetryDelayMilliseconds"] = "10"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services
                .AddHttpClient<IServiceHealthClient, ServiceHealthHttpClient>()
                .ConfigurePrimaryHttpMessageHandler(() => ServiceHealthHandler);

            services.RemoveAll<IIncidentAnalyzer>();
            services.AddSingleton<IIncidentAnalyzer>(IncidentAnalyzer);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
