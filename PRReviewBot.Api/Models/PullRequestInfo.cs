using System.Text.Json.Serialization;

namespace PRReviewBot.Api.Models;

public class PullRequestInfo
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public UserInfo? User { get; set; }
}