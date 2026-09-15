using System.Net;
using System.Net.Http.Json;
using IncidentHub.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IncidentHub.IntegrationTests.Incidents;

public sealed class IncidentsEndpointsTests(IncidentHubWebApplicationFactory factory)
    : IClassFixture<IncidentHubWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    [Fact]
    public async Task Create_ThenGetById_ReturnsPersistedIncident()
    {
        var request = new
        {
            Title = "API unavailable",
            Description = "The payments API is returning errors.",
            Service = "Payments API",
            Severity = "High"
        };

        var postResponse = await client.PostAsJsonAsync("/api/incidents", request);

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        Assert.NotNull(postResponse.Headers.Location);

        var createdIncident = await postResponse.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.NotNull(createdIncident);
        Assert.Equal("Open", createdIncident.Status);

        var getResponse = await client.GetAsync(postResponse.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var returnedIncident = await getResponse.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.NotNull(returnedIncident);
        Assert.Equal(createdIncident, returnedIncident);
    }

    private sealed record IncidentResponse(
        Guid Id,
        string Title,
        string Description,
        string Service,
        string Severity,
        string Status,
        DateTime CreatedAt);
}
