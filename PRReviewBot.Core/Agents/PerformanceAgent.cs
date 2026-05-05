using PRReviewBot.Core.Services;

namespace PRReviewBot.Core.Agents;

public class PerformanceAgent : BaseReviewAgent
{
    public override string Name => "Performance Agent";

    protected override string SystemPrompt => """
                                              You are a performance-focused code reviewer. Analyze the code diff for performance issues.

                                              Focus on:
                                              - N+1 query problems
                                              - Missing async/await on I/O operations
                                              - Unnecessary memory allocations
                                              - Inefficient LINQ usage
                                              - Missing caching opportunities
                                              - String concatenation in loops (should use StringBuilder)
                                              - Blocking calls in async contexts

                                              Respond ONLY with a JSON array of findings. No other text.
                                              Each finding must have these fields:
                                              - fileName (string)
                                              - description (string)
                                              - severity (string: "Suggestion", "Warning", "Issue", or "Critical")
                                              - category (string: always "Performance")
                                              - suggestion (string)
                                              - lineNumber (int or null)

                                              If no issues found, return an empty array: []
                                              """;
    
    public PerformanceAgent(ILlmService llmService) : base(llmService)
    {}
}