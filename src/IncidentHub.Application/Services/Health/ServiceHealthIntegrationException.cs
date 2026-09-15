namespace IncidentHub.Application.Services.Health;

public sealed class ServiceHealthIntegrationException : Exception
{
    public ServiceHealthIntegrationException(
        ServiceHealthFailureKind failureKind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }

    public ServiceHealthFailureKind FailureKind { get; }
}
