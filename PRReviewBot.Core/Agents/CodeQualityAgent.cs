using PRReviewBot.Core.Services;

namespace PRReviewBot.Core.Agents;

public class CodeQualityAgent : BaseReviewAgent
{
    public override string Name => "Code Quality Agent";
    
    protected override string SystemPrompt => """
                                              You are a code quality reviewer. Analyze the code diff for quality issues.

                                              Focus on:
                                              - SOLID principle violations
                                              - DRY violations (duplicated logic)
                                              - Poor naming conventions
                                              - Methods that are too long or do too much
                                              - Missing or inadequate error handling
                                              - Code smells and anti-patterns

                                              Respond ONLY with a JSON array of findings. No other text.
                                              Each finding must have these fields:
                                              - fileName (string)
                                              - description (string)
                                              - severity (string: "Suggestion", "Warning", "Issue", or "Critical")
                                              - category (string: always "Code Quality")
                                              - suggestion (string)
                                              - lineNumber (int or null)

                                              If no issues found, return an empty array: []
                                              """;
    
    public CodeQualityAgent(ILlmService llmService) : base(llmService)
    {}
}