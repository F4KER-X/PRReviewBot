using PRReviewBot.Core.Services;

namespace PRReviewBot.Core.Agents;

public class SummarizerAgent
{
    private readonly ILlmService _llmService;
    
    public SummarizerAgent(ILlmService llmService)
    {
        _llmService = llmService;
    }
    
    public async Task<string> SummarizeAsync(List<ReviewFinding> findings)
    {
        if (findings.Count == 0)
            return "## AI Code Review\n\nNo issues found. Code looks good!";

        var systemPrompt = """
                           You are a code review summarizer. You receive a list of findings from multiple 
                           specialized code review agents. Create a clean, well-organized markdown summary 
                           to post as a GitHub PR comment.

                           Format the summary with:
                           - A header "## AI Code Review"
                           - A brief overview sentence with total finding count
                           - Group findings by severity (Critical first, then Issue, Warning, Suggestion)
                           - Use emoji for severity: 🔴 Critical, 🟠 Issue, 🟡 Warning, 🔵 Suggestion
                           - Include the file name, description, and suggestion for each finding
                           - End with a brief encouraging note

                           Keep it concise and actionable. Developers should be able to scan it quickly.
                           """;

        var findingsText = string.Join("\n", findings.Select(f =>
            $"[{f.Severity}] {f.Category} - {f.FileName}: {f.Description} | Suggestion: {f.Suggestion}"));

        var userPrompt = $"Here are the findings to summarize:\n\n{findingsText}";

        return await _llmService.GetCompletionAsync(systemPrompt, userPrompt);
    }
}