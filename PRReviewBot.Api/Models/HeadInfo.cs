using System.Text.Json.Serialization;

namespace PRReviewBot.Api.Models;

public class HeadInfo
{
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;
}