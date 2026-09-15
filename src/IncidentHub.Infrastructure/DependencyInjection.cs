using IncidentHub.Application.Incidents;
using IncidentHub.Application.Incidents.Analyze;
using IncidentHub.Application.Services.Health;
using IncidentHub.Infrastructure.AI.Ollama;
using IncidentHub.Infrastructure.AI.Rag;
using IncidentHub.Infrastructure.Integrations.ServiceHealth;
using IncidentHub.Infrastructure.Persistence;
using IncidentHub.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OllamaSharp;

namespace IncidentHub.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<IncidentHubDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddServiceHealthIntegration(configuration);

        var ollamaConfiguration = configuration.GetSection("AI:Ollama");
        var ollamaEndpoint = ollamaConfiguration["Endpoint"]
            ?? throw new InvalidOperationException("AI:Ollama:Endpoint is required.");
        var ollamaModel = ollamaConfiguration["Model"]
            ?? throw new InvalidOperationException("AI:Ollama:Model is required.");
        var ollamaTimeout = TimeSpan.FromSeconds(
            ollamaConfiguration.GetValue("TimeoutSeconds", 120));

        services.AddSingleton<IChatClient>(serviceProvider =>
        {
            var toolLogger = serviceProvider
                .GetRequiredService<ILogger<FunctionInvokingChatClient>>();
            return new FunctionInvokingChatClient(
                new OllamaApiClient(new Uri(ollamaEndpoint), ollamaModel),
                serviceProvider.GetRequiredService<ILoggerFactory>(),
                serviceProvider)
            {
                MaximumIterationsPerRequest = 3,
                AllowConcurrentInvocation = false,
                MaximumConsecutiveErrorsPerRequest = 0,
                TerminateOnUnknownCalls = true,
                IncludeDetailedErrors = false,
                FunctionInvoker = async (context, cancellationToken) =>
                {
                    toolLogger.LogInformation(
                        "Invoking AI tool. ToolName: {ToolName}; ToolCallId: {ToolCallId}; Iteration: {Iteration}; FunctionCallIndex: {FunctionCallIndex}",
                        context.CallContent.Name,
                        context.CallContent.CallId,
                        context.Iteration,
                        context.FunctionCallIndex);

                    var result = await context.Function.InvokeAsync(
                        context.Arguments,
                        cancellationToken);

                    toolLogger.LogInformation(
                        "AI tool invocation completed. ToolName: {ToolName}; ToolCallId: {ToolCallId}; ResultType: {ResultType}",
                        context.CallContent.Name,
                        context.CallContent.CallId,
                        result?.GetType().Name);

                    return result;
                }
            };
        });

        var runbooksPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "docs",
        "runbooks");

            services.AddSingleton<IRunbookRetriever>(
                new FileRunbookRetriever(runbooksPath));

        services.AddScoped<IIncidentAnalyzer>(serviceProvider =>
        new OllamaIncidentAnalyzer(
            serviceProvider.GetRequiredService<IChatClient>(),
            serviceProvider.GetRequiredService<ServiceHealthLookup>(),
            serviceProvider.GetRequiredService<IRunbookRetriever>(),
            ollamaTimeout,
            serviceProvider.GetRequiredService<ILogger<OllamaIncidentAnalyzer>>()));

        return services;
    }
}
