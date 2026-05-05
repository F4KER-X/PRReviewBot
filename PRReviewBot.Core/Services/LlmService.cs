using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace PRReviewBot.Core.Services;

public class LlmService : ILlmService
{
    private readonly IChatCompletionService _chatCompletion;

    public LlmService(Kernel  kernel)
    {
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
    }
    
    public async Task<string> GetCompletionAsync(string systemPrompt, string userPrompt)
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(systemPrompt);
        chatHistory.AddUserMessage(userPrompt);
        var response = await _chatCompletion.GetChatMessageContentAsync(chatHistory);
        return response.Content ?? string.Empty;
    }
}