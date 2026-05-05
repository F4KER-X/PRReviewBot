using System.Text.Json.Serialization;
namespace PRReviewBot.Api.Models;

public class GitHubWebhookPayload
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("pull_request")]
    public PullRequestInfo? PullRequest { get; set; }

    [JsonPropertyName("repository")]
    public RepositoryInfo? Repository { get; set; }
}
