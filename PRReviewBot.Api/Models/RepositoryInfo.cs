using System.Text.Json.Serialization;

namespace PRReviewBot.Api.Models;

public class RepositoryInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("owner")]
    public OwnerInfo? Owner { get; set; }
}