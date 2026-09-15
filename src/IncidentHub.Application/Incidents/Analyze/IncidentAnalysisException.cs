namespace IncidentHub.Application.Incidents.Analyze;

public sealed class IncidentAnalysisException : Exception
{
    public IncidentAnalysisException(
        IncidentAnalysisFailureKind failureKind,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
    }

    public IncidentAnalysisFailureKind FailureKind { get; }
}
