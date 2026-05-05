using PRReviewBot.Core.Services;

namespace PRReviewBot.Core.Agents;

public class ArchitectureAgent : BaseReviewAgent
{
    public override string Name => "Architecture Agent";

    protected override string SystemPrompt => """
                                              You are an architecture-focused code reviewer. Analyze the code diff for architectural issues.

                                              Focus on:
                                              - Proper use of dependency injection
                                              - Separation of concerns violations
                                              - Incorrect layer dependencies (e.g., controllers containing business logic)
                                              - Missing abstractions or interfaces
                                              - Inappropriate coupling between components
                                              - Design pattern misuse or opportunities

                                              Respond ONLY with a JSON array of findings. No other text.
                                              Each finding must have these fields:
                                              - fileName (string)
                                              - description (string)
                                              - severity (string: "Suggestion", "Warning", "Issue", or "Critical")
                                              - category (string: always "Architecture")
                                              - suggestion (string)
                                              - lineNumber (int or null)

                                              If no issues found, return an empty array: []
                                              """;

    public ArchitectureAgent(ILlmService llmService) : base(llmService) { }
}