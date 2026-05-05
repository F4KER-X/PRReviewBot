using Moq;
using PRReviewBot.Core.Agents;
using PRReviewBot.Core.Services;

namespace PRReviewBot.Tests.Agents;

public class ReviewFindingParsingTests
{
    private readonly Mock<ILlmService> _mockLlm;
    private readonly SecurityAgent _agent;

    public ReviewFindingParsingTests()
    {
        _mockLlm = new Mock<ILlmService>();
        _agent = new SecurityAgent(_mockLlm.Object);
    }

    [Fact]
    public async Task ReviewAsync_WithValidJsonResponse_ReturnsFindings()
    {
        var jsonResponse = """
            [
                {
                    "fileName": "test.cs",
                    "description": "SQL injection found",
                    "severity": "Critical",
                    "category": "Security",
                    "suggestion": "Use parameterized queries",
                    "lineNumber": 5
                }
            ]
            """;

        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(jsonResponse);

        var findings = await _agent.ReviewAsync("some diff");

        Assert.Single(findings);
        Assert.Equal("test.cs", findings[0].FileName);
        Assert.Equal(Severity.Critical, findings[0].Severity);
    }

    [Fact]
    public async Task ReviewAsync_WithMarkdownWrappedJson_ReturnsFindings()
    {
        var response = """
            [
                {
                    "fileName": "test.cs",
                    "description": "Hardcoded password",
                    "severity": "Warning",
                    "category": "Security",
                    "suggestion": "Use environment variables",
                    "lineNumber": 10
                }
            ]
            """;

        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        var findings = await _agent.ReviewAsync("some diff");

        Assert.Single(findings);
        Assert.Equal(Severity.Warning, findings[0].Severity);
    }

    [Fact]
    public async Task ReviewAsync_WithEmptyArray_ReturnsNoFindings()
    {
        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("[]");

        var findings = await _agent.ReviewAsync("some diff");

        Assert.Empty(findings);
    }

    [Fact]
    public async Task ReviewAsync_WithGarbageResponse_ReturnsNoFindings()
    {
        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("I couldn't understand the code, here are some thoughts...");

        var findings = await _agent.ReviewAsync("some diff");

        Assert.Empty(findings);
    }

    [Fact]
    public async Task ReviewAsync_WithNullLineNumber_ParsesCorrectly()
    {
        var response = """
            [
                {
                    "fileName": "test.cs",
                    "description": "Missing abstraction",
                    "severity": "Suggestion",
                    "category": "Architecture",
                    "suggestion": "Extract interface",
                    "lineNumber": null
                }
            ]
            """;

        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(response);

        var findings = await _agent.ReviewAsync("some diff");

        Assert.Single(findings);
        Assert.Null(findings[0].LineNumber);
    }
}