using System.Net;
using System.Net.Http.Json;
using IncidentHub.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IncidentHub.IntegrationTests.Incidents;

public sealed class ListIncidentsTests(IncidentHubWebApplicationFactory factory)
    : IClassFixture<IncidentHubWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });

    [Fact]
    public async Task List_WithFiltersAndPagination_ReturnsMatchingPage()
    {
        var resolvedId = await CreateIncidentAsync("Resolved incident", "Payments API", "High");
        await ChangeStatusAsync(resolvedId, "Resolved");

        await CreateIncidentAsync("Older open incident", "Payments API", "High");
        var expectedId = await CreateIncidentAsync("Newest open incident", "Payments API", "High");
        await CreateIncidentAsync("Different severity", "Payments API", "Critical");
        await CreateIncidentAsync("Different service", "Billing API", "High");

        var response = await client.GetAsync(
            "/api/incidents?page=1&pageSize=1&status=Open&severity=High&service=Payments%20API");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var page = await response.Content.ReadFromJsonAsync<PagedIncidentResponse>();
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal(expectedId, page.Items[0].Id);
        Assert.Equal(1, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task List_WithInvalidPage_ReturnsBadRequest()
    {
        var response = await client.GetAsync("/api/incidents?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> CreateIncidentAsync(string title, string service, string severity)
    {
        var response = await client.PostAsJsonAsync("/api/incidents", new
        {
            Title = title,
            Description = "Integration test incident.",
            Service = service,
            Severity = severity
        });

        response.EnsureSuccessStatusCode();
        var incident = await response.Content.ReadFromJsonAsync<IncidentResponse>();
        return incident!.Id;
    }

    private async Task ChangeStatusAsync(Guid id, string status)
    {
        var response = await client.PatchAsJsonAsync($"/api/incidents/{id}/status", new
        {
            Status = status
        });

        response.EnsureSuccessStatusCode();
    }

    private sealed record PagedIncidentResponse(
        IReadOnlyList<IncidentResponse> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);

    private sealed record IncidentResponse(
        Guid Id,
        string Title,
        string Description,
        string Service,
        string Severity,
        string Status,
        DateTime CreatedAt);
}
