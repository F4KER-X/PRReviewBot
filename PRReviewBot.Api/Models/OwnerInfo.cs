using System.Text.Json.Serialization;

namespace PRReviewBot.Api.Models;

public class OwnerInfo
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
}