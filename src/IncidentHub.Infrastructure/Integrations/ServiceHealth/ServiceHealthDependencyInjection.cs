using System.Net;
using IncidentHub.Application.Services.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;

namespace IncidentHub.Infrastructure.Integrations.ServiceHealth;

public static class ServiceHealthDependencyInjection
{
    public static IServiceCollection AddServiceHealthIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var httpClientBuilder = services.AddHttpClient<IServiceHealthClient, ServiceHealthHttpClient>(
            (serviceProvider, client) =>
            {
                var clientConfiguration = serviceProvider
                    .GetRequiredService<IConfiguration>()
                    .GetSection("ExternalServices:ServiceHealth");
                var baseUrl = clientConfiguration["BaseUrl"]
                    ?? throw new InvalidOperationException(
                        "ExternalServices:ServiceHealth:BaseUrl is required.");

                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = Timeout.InfiniteTimeSpan;
            });

        httpClientBuilder.AddResilienceHandler(
            "service-health",
            (pipelineBuilder, context) =>
            {
                var resilienceConfiguration = context.ServiceProvider
                    .GetRequiredService<IConfiguration>()
                    .GetSection("ExternalServices:ServiceHealth");
                var attemptTimeout = TimeSpan.FromMilliseconds(
                    resilienceConfiguration.GetValue(
                        "AttemptTimeoutMilliseconds",
                        1_000));
                var totalTimeout = TimeSpan.FromMilliseconds(
                    resilienceConfiguration.GetValue(
                        "TotalTimeoutMilliseconds",
                        3_000));
                var retryDelay = TimeSpan.FromMilliseconds(
                    resilienceConfiguration.GetValue(
                        "RetryDelayMilliseconds",
                        200));
                var logger = context.ServiceProvider
                    .GetRequiredService<ILogger<ServiceHealthHttpClient>>();

                pipelineBuilder
                    .AddTimeout(totalTimeout)
                    .AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 2,
                        Delay = retryDelay,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        ShouldRetryAfterHeader = true,
                        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                            .Handle<HttpRequestException>()
                            .Handle<TimeoutRejectedException>()
                            .HandleResult(response =>
                                ServiceHealthHttpClient.IsTransientStatusCode(
                                    response.StatusCode)),
                        OnRetry = arguments =>
                        {
                            var failure = arguments.Outcome.Exception?.GetType().Name
                                ?? arguments.Outcome.Result?.StatusCode.ToString()
                                ?? "Unknown";

                            logger.LogWarning(
                                "Retrying service health request. RetryAttempt: {RetryAttempt}; DelayMilliseconds: {DelayMilliseconds}; Failure: {Failure}",
                                arguments.AttemptNumber + 1,
                                arguments.RetryDelay.TotalMilliseconds,
                                failure);

                            return ValueTask.CompletedTask;
                        }
                    })
                    .AddTimeout(attemptTimeout);
            });

        return services;
    }
}
