using System.Runtime.CompilerServices;
using IncidentHub.Application.Incidents.Analyze;
using IncidentHub.Application.Services.Health;
using IncidentHub.Domain.Incidents;
using IncidentHub.Infrastructure.AI.Ollama;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace IncidentHub.IntegrationTests.Infrastructure;

public sealed class OllamaIncidentAnalyzerTests
{
    [Fact]
    public async Task Analyze_ValidStructuredOutput_UsesApplicationIncidentId()
    {
        const string json = """
            {
              "summary": "Payments are failing.",
              "facts": ["HTTP 500 was reported."],
              "hypotheses": ["A dependency may be failing."],
              "recommendedActions": ["Inspect application logs."],
              "confidence": 0.65,
              "incidentId": "00000000-0000-0000-0000-000000000000"
            }
            """;
        using var chatClient = new StubChatClient(json);
        var analyzer = CreateAnalyzer(chatClient);
        var incidentId = Guid.NewGuid();

        var result = await analyzer.AnalyzeAsync(CreateInput(incidentId), default);

        Assert.Equal(incidentId, result.IncidentId);
        Assert.Equal(0.65, result.Confidence);
        Assert.Equal(4, chatClient.Messages.Count);
        Assert.Equal(ChatRole.System, chatClient.Messages[0].Role);
        Assert.Equal(ChatRole.User, chatClient.Messages[1].Role);
        Assert.Null(chatClient.RequestOptions[0]?.ResponseFormat);
        var tool = Assert.Single(chatClient.RequestOptions[0]?.Tools ?? []);
        Assert.Equal("get_service_health", tool.Name);
        Assert.False(chatClient.RequestOptions[0]?.AllowMultipleToolCalls);
        Assert.NotNull(chatClient.RequestOptions[1]?.ResponseFormat);
        Assert.Empty(chatClient.RequestOptions[1]?.Tools ?? []);
    }

    [Theory]
    [InlineData("", 0.5)]
    [InlineData("Summary", -0.1)]
    [InlineData("Summary", 1.1)]
    public async Task Analyze_InvalidRequiredValues_ThrowsInvalidResponse(
        string summary,
        double confidence)
    {
        var json = $$"""
            {
              "summary": "{{summary}}",
              "facts": [],
              "hypotheses": [],
              "recommendedActions": [],
              "confidence": {{confidence.ToString(System.Globalization.CultureInfo.InvariantCulture)}}
            }
            """;
        using var chatClient = new StubChatClient(json);
        var analyzer = CreateAnalyzer(chatClient);

        var exception = await Assert.ThrowsAsync<IncidentAnalysisException>(
            () => analyzer.AnalyzeAsync(CreateInput(Guid.NewGuid()), default));

        Assert.Equal(IncidentAnalysisFailureKind.InvalidResponse, exception.FailureKind);
    }

    [Fact]
    public async Task Analyze_CallerCancellation_IsPropagated()
    {
        using var chatClient = new StubChatClient("{}", cancel: true);
        var analyzer = CreateAnalyzer(chatClient);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => analyzer.AnalyzeAsync(CreateInput(Guid.NewGuid()), cancellation.Token));
    }

    [Fact]
    public async Task Analyze_ModelCallsHealthTool_ReturnsResultToModel()
    {
        using var innerClient = new ToolCallingChatClient(
            FinalResponseJson,
            [new FunctionCallContent(
                "health-call-1",
                "get_service_health",
                new Dictionary<string, object?> { ["serviceName"] = "Payments API" })]);
        using var functionClient = CreateFunctionInvokingClient(innerClient);
        var healthClient = new StubServiceHealthClient(
            new ServiceHealthDto(
                "Payments API",
                ServiceHealthStatus.Degraded,
                new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc)));
        var analyzer = CreateAnalyzer(functionClient, healthClient);

        var result = await analyzer.AnalyzeAsync(CreateInput(Guid.NewGuid()), default);

        Assert.Equal(0.72, result.Confidence);
        Assert.Equal(1, healthClient.CallCount);
        Assert.Equal("Payments API", healthClient.LastServiceName);

        var secondRequest = Assert.IsType<ChatMessage[]>(innerClient.Requests[1]);
        var functionCall = Assert.Single(
            secondRequest.SelectMany(message => message.Contents)
                .OfType<FunctionCallContent>());
        Assert.Equal("health-call-1", functionCall.CallId);
        Assert.Equal("get_service_health", functionCall.Name);

        var functionResult = Assert.Single(
            secondRequest.SelectMany(message => message.Contents)
                .OfType<FunctionResultContent>());
        Assert.Equal("health-call-1", functionResult.CallId);
        var resultJson = System.Text.Json.JsonSerializer.Serialize(functionResult.Result);
        Assert.Contains("Degraded", resultJson);
        Assert.Contains("Payments API", resultJson);
    }

    [Fact]
    public async Task Analyze_ModelRequestsDifferentService_DoesNotCallIntegration()
    {
        using var innerClient = new ToolCallingChatClient(
            FinalResponseJson,
            [new FunctionCallContent(
                "health-call-1",
                "get_service_health",
                new Dictionary<string, object?> { ["serviceName"] = "Admin API" })]);
        using var functionClient = CreateFunctionInvokingClient(innerClient);
        var healthClient = new StubServiceHealthClient(null);
        var analyzer = CreateAnalyzer(functionClient, healthClient);

        await analyzer.AnalyzeAsync(CreateInput(Guid.NewGuid()), default);

        Assert.Equal(0, healthClient.CallCount);
        var functionResult = Assert.Single(
            innerClient.Requests[1].SelectMany(message => message.Contents)
                .OfType<FunctionResultContent>());
        var resultJson = System.Text.Json.JsonSerializer.Serialize(functionResult.Result);
        Assert.Contains("service_not_allowed", resultJson);
    }

    [Fact]
    public async Task Analyze_MoreThanTwoToolCalls_StopsBeforeThirdIntegrationCall()
    {
        FunctionCallContent[] calls =
        [
            CreateHealthCall("health-call-1"),
            CreateHealthCall("health-call-2"),
            CreateHealthCall("health-call-3")
        ];
        using var innerClient = new ToolCallingChatClient(FinalResponseJson, calls);
        using var functionClient = CreateFunctionInvokingClient(innerClient);
        var healthClient = new StubServiceHealthClient(
            new ServiceHealthDto("Payments API", ServiceHealthStatus.Healthy, DateTime.UtcNow));
        var analyzer = CreateAnalyzer(functionClient, healthClient);

        var exception = await Assert.ThrowsAsync<IncidentAnalysisException>(
            () => analyzer.AnalyzeAsync(CreateInput(Guid.NewGuid()), default));

        Assert.Equal(IncidentAnalysisFailureKind.InvalidResponse, exception.FailureKind);
        Assert.Equal(2, healthClient.CallCount);
    }

    private static OllamaIncidentAnalyzer CreateAnalyzer(
    IChatClient chatClient,
    IServiceHealthClient? healthClient = null) =>
    new(
        chatClient,
        new ServiceHealthLookup(
            healthClient ?? new StubServiceHealthClient(null)),
        new StubRunbookRetriever(),
        TimeSpan.FromSeconds(5),
        NullLogger<OllamaIncidentAnalyzer>.Instance);

    private static FunctionInvokingChatClient CreateFunctionInvokingClient(IChatClient innerClient) =>
        new(innerClient, NullLoggerFactory.Instance, null)
        {
            MaximumIterationsPerRequest = 3,
            AllowConcurrentInvocation = false,
            MaximumConsecutiveErrorsPerRequest = 0,
            TerminateOnUnknownCalls = true,
            IncludeDetailedErrors = false
        };

    private static FunctionCallContent CreateHealthCall(string callId) =>
        new(
            callId,
            "get_service_health",
            new Dictionary<string, object?> { ["serviceName"] = "Payments API" });

    private static IncidentAnalysisInput CreateInput(Guid incidentId) =>
        new(
            incidentId,
            "Payments unavailable",
            "Clients report HTTP 500.",
            "Payments API",
            IncidentSeverity.High,
            IncidentStatus.Open,
            DateTime.UtcNow);

    private const string FinalResponseJson = """
        {
          "summary": "Payments are degraded.",
          "facts": ["The health integration reported Degraded."],
          "hypotheses": ["A dependency may be impaired."],
          "recommendedActions": ["Inspect service metrics."],
          "confidence": 0.72
        }
        """;

    private sealed class StubServiceHealthClient(ServiceHealthDto? response)
        : IServiceHealthClient
    {
        public int CallCount { get; private set; }

        public string? LastServiceName { get; private set; }

        public Task<ServiceHealthDto?> GetHealthAsync(
            string serviceName,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastServiceName = serviceName;
            return Task.FromResult(response);
        }
    }

    private sealed class ToolCallingChatClient(
        string finalResponse,
        IReadOnlyList<FunctionCallContent> calls) : IChatClient
    {
        private int requestCount;

        public List<ChatMessage[]> Requests { get; } = [];

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(messages.ToArray());

            var response = requestCount++ == 0
                ? new ChatMessage(ChatRole.Assistant, calls.Cast<AIContent>().ToArray())
                : new ChatMessage(ChatRole.Assistant, finalResponse);

            return Task.FromResult(new ChatResponse(response));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }

    private sealed class StubChatClient(string response, bool cancel = false) : IChatClient
    {
        public List<ChatMessage> Messages { get; } = [];

        public ChatOptions? Options { get; private set; }

        public List<ChatOptions?> RequestOptions { get; } = [];

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Messages.AddRange(messages);
            Options = options;
            RequestOptions.Add(options);

            if (cancel)
            {
                return Task.FromCanceled<ChatResponse>(cancellationToken);
            }

            return Task.FromResult(
                new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }
}
