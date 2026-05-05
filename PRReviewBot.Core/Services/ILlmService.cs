namespace PRReviewBot.Core.Services;

public interface ILlmService
{
    Task<string> GetCompletionAsync(string systemPrompt, string userPrompt);
}