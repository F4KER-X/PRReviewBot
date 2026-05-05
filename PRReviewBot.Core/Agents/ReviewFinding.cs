using System.Text.Json.Serialization;

namespace PRReviewBot.Core.Agents;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Severity
{
    Suggestion,
    Warning,
    Issue,
    Critical
}

public class ReviewFinding
{
    public string FileName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Severity Severity { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
    public int? LineNumber { get; set; }
}