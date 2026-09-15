using System.ComponentModel;
using System.Text.Json.Serialization;

namespace IncidentHub.Infrastructure.AI.Ollama;

internal sealed record OllamaIncidentAnalysisOutput(
    [property: JsonPropertyName("summary")]
    [property: Description("Concise summary grounded only in the supplied incident data.")]
    string Summary,
    [property: JsonPropertyName("facts")]
    [property: Description("Facts explicitly present in the supplied incident data.")]
    string[] Facts,
    [property: JsonPropertyName("hypotheses")]
    [property: Description("Possible explanations, clearly expressed as hypotheses rather than facts.")]
    string[] Hypotheses,
    [property: JsonPropertyName("recommendedActions")]
    [property: Description("Concrete next investigation actions, without claiming they were already performed.")]
    string[] RecommendedActions,
    [property: JsonPropertyName("confidence")]
    [property: Description("Confidence in the analysis, between 0 and 1 inclusive.")]
    double Confidence);
