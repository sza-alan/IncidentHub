using System.Net;
using System.Net.Http.Json;
using IncidentHub.Application.Incidents.Analyze;
using IncidentHub.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IncidentHub.IntegrationTests.Incidents;

public sealed class AnalyzeIncidentTests : IClassFixture<IncidentHubWebApplicationFactory>
{
    private readonly IncidentHubWebApplicationFactory factory;
    private readonly HttpClient client;

    public AnalyzeIncidentTests(IncidentHubWebApplicationFactory factory)
    {
        this.factory = factory;
        factory.IncidentAnalyzer.Reset();
        client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task Analyze_ExistingIncident_ReturnsStructuredAnalysis()
    {
        var incidentId = await CreateIncidentAsync();

        var response = await client.PostAsync($"/api/incidents/{incidentId}/analyze", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var analysis = await response.Content.ReadFromJsonAsync<IncidentAnalysisDto>();
        Assert.NotNull(analysis);
        Assert.Equal(incidentId, analysis.IncidentId);
        Assert.NotEmpty(analysis.Summary);
        Assert.NotEmpty(analysis.Facts);
        Assert.NotEmpty(analysis.Hypotheses);
        Assert.NotEmpty(analysis.RecommendedActions);
        Assert.InRange(analysis.Confidence, 0, 1);

        Assert.Equal(incidentId, factory.IncidentAnalyzer.LastInput?.IncidentId);
        Assert.Equal("Payments API", factory.IncidentAnalyzer.LastInput?.Service);
        Assert.True(factory.IncidentAnalyzer.ReceivedCancelableToken);
    }

    [Fact]
    public async Task Analyze_MissingIncident_ReturnsNotFoundWithoutCallingAnalyzer()
    {
        var response = await client.PostAsync(
            $"/api/incidents/{Guid.NewGuid()}/analyze",
            null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(factory.IncidentAnalyzer.LastInput);
    }

    [Theory]
    [InlineData(IncidentAnalysisFailureKind.InvalidResponse, HttpStatusCode.BadGateway)]
    [InlineData(IncidentAnalysisFailureKind.Unavailable, HttpStatusCode.ServiceUnavailable)]
    [InlineData(IncidentAnalysisFailureKind.Timeout, HttpStatusCode.GatewayTimeout)]
    public async Task Analyze_IntegrationFailure_ReturnsExpectedStatus(
        IncidentAnalysisFailureKind failureKind,
        HttpStatusCode expectedStatus)
    {
        var incidentId = await CreateIncidentAsync();
        factory.IncidentAnalyzer.FailureKind = failureKind;

        var response = await client.PostAsync($"/api/incidents/{incidentId}/analyze", null);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    private async Task<Guid> CreateIncidentAsync()
    {
        var response = await client.PostAsJsonAsync("/api/incidents", new
        {
            Title = "API unavailable",
            Description = "The payments API is returning errors.",
            Service = "Payments API",
            Severity = "High"
        });
        response.EnsureSuccessStatusCode();

        var incident = await response.Content.ReadFromJsonAsync<IncidentResponse>();
        return incident!.Id;
    }

    private sealed record IncidentResponse(Guid Id);
}
