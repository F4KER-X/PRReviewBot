using Moq;
using PRReviewBot.Core.Agents;
using PRReviewBot.Core.Services;

namespace PRReviewBot.Tests.Agents;

public class SummarizerAgentTests
{
    private readonly Mock<ILlmService> _mockLlm = new();
    private readonly SummarizerAgent _summarizer;

    public SummarizerAgentTests()
    {
        _summarizer = new SummarizerAgent(_mockLlm.Object);
    }

    [Fact]
    public async Task SummarizeAsync_WithEmptyList_DoesNotCallLlm()
    {
        await _summarizer.SummarizeAsync([]);

        _mockLlm.Verify(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SummarizeAsync_WithEmptyList_ReturnsNoIssuesMessage()
    {
        var result = await _summarizer.SummarizeAsync([]);

        Assert.Contains("## AI Code Review", result);
        Assert.Contains("No issues found", result);
    }

    [Fact]
    public async Task SummarizeAsync_WithFindings_CallsLlmOnce()
    {
        var findings = new List<ReviewFinding>
        {
            new() { FileName = "auth.cs", Description = "Hardcoded secret", Severity = Severity.Critical, Category = "Security", Suggestion = "Use secrets manager" }
        };

        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("## AI Code Review\n\nFound 1 issue.");

        await _summarizer.SummarizeAsync(findings);

        _mockLlm.Verify(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SummarizeAsync_WithFindings_ReturnsLlmResponse()
    {
        var findings = new List<ReviewFinding>
        {
            new() { FileName = "test.cs", Description = "Issue", Severity = Severity.Warning, Category = "Quality", Suggestion = "Fix it" }
        };
        const string expected = "## AI Code Review\n\nFound issues.";

        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expected);

        var result = await _summarizer.SummarizeAsync(findings);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task SummarizeAsync_WithFindings_IncludesFindingDetailsInUserPrompt()
    {
        var findings = new List<ReviewFinding>
        {
            new() { FileName = "auth.cs", Description = "Hardcoded secret", Severity = Severity.Critical, Category = "Security", Suggestion = "Use secrets manager" }
        };

        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("summary");

        await _summarizer.SummarizeAsync(findings);

        _mockLlm.Verify(x => x.GetCompletionAsync(
            It.IsAny<string>(),
            It.Is<string>(p => p.Contains("auth.cs") && p.Contains("Hardcoded secret") && p.Contains("Use secrets manager"))),
            Times.Once);
    }
}
