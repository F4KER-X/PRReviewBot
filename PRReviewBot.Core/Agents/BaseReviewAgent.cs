using System.Text.Json;
using PRReviewBot.Core.Services;

namespace PRReviewBot.Core.Agents;

public abstract class BaseReviewAgent : IReviewAgent
{
    protected readonly ILlmService _llmService;

    public abstract string Name { get; }
    protected abstract string SystemPrompt { get; }
    
    protected BaseReviewAgent(ILlmService llmService)
    {
        _llmService = llmService;
    }
    
    public async Task<List<ReviewFinding>> ReviewAsync(string diff)
    {
        var userPrompt = $"Review the following code diff and provide your findings:\n\n{diff}";
        var response = await _llmService.GetCompletionAsync(SystemPrompt, userPrompt);
        return ParseFindings(response);
    }
    
    private List<ReviewFinding> ParseFindings(string response)
    {
        try
        {
            // Strip markdown code blocks
            var cleaned = response
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var jsonStart = cleaned.IndexOf('[');
            var jsonEnd = cleaned.LastIndexOf(']');

            if (jsonStart == -1 || jsonEnd == -1)
                return new List<ReviewFinding>();

            var json = cleaned.Substring(jsonStart, jsonEnd - jsonStart + 1);

            var findings = JsonSerializer.Deserialize<List<ReviewFinding>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return findings ?? new List<ReviewFinding>();
        }
        catch
        {
            return new List<ReviewFinding>();
        }
    }
}