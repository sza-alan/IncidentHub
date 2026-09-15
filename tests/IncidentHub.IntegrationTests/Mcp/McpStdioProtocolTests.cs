using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit.Abstractions;

namespace IncidentHub.IntegrationTests.Mcp;

public sealed class McpStdioProtocolTests(ITestOutputHelper output)
{
    [Fact]
    public async Task StdioServer_ListsOnlyHealthTool_AndCallsIt()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var healthServer = await SingleResponseHttpServer.StartAsync(
            """
            {"service":"Payments API","status":"operational"}
            """,
            cancellation.Token);

        var repositoryRoot = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var serverAssemblyPath = Path.Combine(
            repositoryRoot,
            "src",
            "IncidentHub.Mcp",
            "bin",
            "Debug",
            "net10.0",
            "IncidentHub.Mcp.dll");
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "incidenthub-integration-test",
            Command = "dotnet",
            Arguments = [serverAssemblyPath],
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["ExternalServices__ServiceHealth__BaseUrl"] = healthServer.BaseUrl,
                ["ExternalServices__ServiceHealth__AttemptTimeoutMilliseconds"] = "1000",
                ["ExternalServices__ServiceHealth__TotalTimeoutMilliseconds"] = "3000",
                ["ExternalServices__ServiceHealth__RetryDelayMilliseconds"] = "10"
            },
            ShutdownTimeout = TimeSpan.FromSeconds(5)
        });

        await using var client = await McpClient.CreateAsync(
            transport,
            cancellationToken: cancellation.Token);

        var tools = await client.ListToolsAsync(cancellationToken: cancellation.Token);
        var tool = Assert.Single(tools);
        Assert.Equal("get_service_health", tool.Name);
        Assert.Contains("serviceName", tool.JsonSchema.GetRawText());

        var result = await client.CallToolAsync(
            tool.Name,
            new Dictionary<string, object?>
            {
                ["serviceName"] = "Payments API"
            },
            cancellationToken: cancellation.Token);

        var structuredContent = Assert.IsType<JsonElement>(result.StructuredContent);
        Assert.True(structuredContent.GetProperty("succeeded").GetBoolean());
        Assert.Equal(
            "Payments API",
            structuredContent.GetProperty("serviceName").GetString());
        Assert.Equal("Healthy", structuredContent.GetProperty("status").GetString());

        output.WriteLine($"tools/list: {JsonSerializer.Serialize(tools.Select(item => new { item.Name, Schema = item.JsonSchema }))}");
        output.WriteLine($"tools/call: {structuredContent.GetRawText()}");
    }

    private sealed class SingleResponseHttpServer : IAsyncDisposable
    {
        private readonly TcpListener listener;
        private readonly Task serverTask;

        private SingleResponseHttpServer(
            TcpListener listener,
            string responseBody,
            CancellationToken cancellationToken)
        {
            this.listener = listener;
            BaseUrl = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/";
            serverTask = ServeAsync(listener, responseBody, cancellationToken);
        }

        public string BaseUrl { get; }

        public static Task<SingleResponseHttpServer> StartAsync(
            string responseBody,
            CancellationToken cancellationToken)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return Task.FromResult(
                new SingleResponseHttpServer(listener, responseBody, cancellationToken));
        }

        public async ValueTask DisposeAsync()
        {
            listener.Stop();

            try
            {
                await serverTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
        }

        private static async Task ServeAsync(
            TcpListener listener,
            string responseBody,
            CancellationToken cancellationToken)
        {
            using var client = await listener.AcceptTcpClientAsync(cancellationToken);
            await using var stream = client.GetStream();
            using var reader = new StreamReader(
                stream,
                Encoding.ASCII,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            while (!string.IsNullOrEmpty(await reader.ReadLineAsync(cancellationToken)))
            {
            }

            var bodyBytes = Encoding.UTF8.GetBytes(responseBody);
            var headers = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(headers, cancellationToken);
            await stream.WriteAsync(bodyBytes, cancellationToken);
        }
    }
}
