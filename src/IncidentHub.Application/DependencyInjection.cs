using Microsoft.Extensions.DependencyInjection;
using IncidentHub.Application.Services.Health;

namespace IncidentHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddScoped<ServiceHealthLookup>();

        return services;
    }
}
