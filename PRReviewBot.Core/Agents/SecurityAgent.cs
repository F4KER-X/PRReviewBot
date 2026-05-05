using PRReviewBot.Core.Services;

namespace PRReviewBot.Core.Agents;

public class SecurityAgent : BaseReviewAgent
{
    public override string Name => "Security Agent";
    protected override string SystemPrompt => """
                                              You are a security-focused code reviewer. Analyze the code diff for security vulnerabilities.

                                              Focus on:
                                              - SQL injection vulnerabilities
                                              - Hardcoded secrets, API keys, or passwords
                                              - Missing input validation or sanitization
                                              - Authentication and authorization issues
                                              - Insecure data exposure
                                              - Cross-site scripting (XSS) potential

                                              Respond ONLY with a JSON array of findings. No other text.
                                              Each finding must have these fields:
                                              - fileName (string)
                                              - description (string) 
                                              - severity (string: "Suggestion", "Warning", "Issue", or "Critical")
                                              - category (string: always "Security")
                                              - suggestion (string)
                                              - lineNumber (int or null)

                                              If no issues found, return an empty array: []
                                              """;
    
    public SecurityAgent(ILlmService llmService) : base(llmService)
    {}
}