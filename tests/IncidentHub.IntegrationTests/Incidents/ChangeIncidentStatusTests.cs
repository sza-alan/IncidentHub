using System.Net;
using System.Net.Http.Json;
using IncidentHub.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IncidentHub.IntegrationTests.Incidents;

public sealed class ChangeIncidentStatusTests(IncidentHubWebApplicationFactory factory)
    : IClassFixture<IncidentHubWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    [Fact]
    public async Task ChangeStatus_WithAllowedTransition_PersistsStatus()
    {
        var incidentId = await CreateIncidentAsync();

        var patchResponse = await ChangeStatusAsync(incidentId, "InProgress");

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var updatedIncident = await patchResponse.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.NotNull(updatedIncident);
        Assert.Equal("InProgress", updatedIncident.Status);

        var getResponse = await client.GetAsync($"/api/incidents/{incidentId}");
        var persistedIncident = await getResponse.Content.ReadFromJsonAsync<IncidentResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(persistedIncident);
        Assert.Equal("InProgress", persistedIncident.Status);
    }

    [Fact]
    public async Task ChangeStatus_WhenIncidentDoesNotExist_ReturnsNotFound()
    {
        var response = await ChangeStatusAsync(Guid.NewGuid(), "InProgress");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_WithInvalidTransition_ReturnsConflict()
    {
        var incidentId = await CreateIncidentAsync();
        var resolveResponse = await ChangeStatusAsync(incidentId, "Resolved");
        resolveResponse.EnsureSuccessStatusCode();

        var response = await ChangeStatusAsync(incidentId, "InProgress");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_WithoutStatus_ReturnsBadRequest()
    {
        var incidentId = await CreateIncidentAsync();

        var response = await client.PatchAsJsonAsync(
            $"/api/incidents/{incidentId}/status",
            new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> CreateIncidentAsync()
    {
        var response = await client.PostAsJsonAsync("/api/incidents", new
        {
            Title = "API unavailable",
            Description = "Integration test incident.",
            Service = "Payments API",
            Severity = "High"
        });

        response.EnsureSuccessStatusCode();
        var incident = await response.Content.ReadFromJsonAsync<IncidentResponse>();
        return incident!.Id;
    }

    private Task<HttpResponseMessage> ChangeStatusAsync(Guid id, string status) =>
        client.PatchAsJsonAsync($"/api/incidents/{id}/status", new { Status = status });

    private sealed record IncidentResponse(
        Guid Id,
        string Title,
        string Description,
        string Service,
        string Severity,
        string Status,
        DateTime CreatedAt);
}
