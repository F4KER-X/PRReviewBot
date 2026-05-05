using Moq;
using PRReviewBot.Core.Agents;
using PRReviewBot.Core.Orchestration;
using PRReviewBot.Core.Services;

namespace PRReviewBot.Tests.Orchestration;

public class ReviewOrchestratorAdditionalTests
{
    private readonly Mock<IGitHubService> _mockGitHub = new();
    private readonly Mock<ILlmService> _mockLlm = new();

    [Fact]
    public async Task RunReviewAsync_WithWhitespaceOnlyDiff_PostsNoChangesMessage()
    {
        _mockGitHub.Setup(x => x.GetPullRequestDiffAsync("owner", "repo", 1))
            .ReturnsAsync("   \n  \t  ");

        await CreateOrchestrator().RunReviewAsync("owner", "repo", 1);

        _mockGitHub.Verify(x => x.PostReviewCommentAsync(
            "owner", "repo", 1,
            It.Is<string>(s => s.Contains("No code changes detected"))),
            Times.Once);
    }

    [Fact]
    public async Task RunReviewAsync_WithImageFiles_FiltersThemOut()
    {
        var diff = "=== logo.png ===\nbinary content\n\n=== app.cs ===\n+public class App { }";

        _mockGitHub.Setup(x => x.GetPullRequestDiffAsync("owner", "repo", 1))
            .ReturnsAsync(diff);
        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("[]");

        await CreateOrchestrator().RunReviewAsync("owner", "repo", 1);

        _mockLlm.Verify(x => x.GetCompletionAsync(
            It.IsAny<string>(),
            It.Is<string>(s => !s.Contains("logo.png"))),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunReviewAsync_WithLockFiles_FiltersThemOut()
    {
        // yarn.lock has extension .lock which is in the excluded list
        var diff = "=== yarn.lock ===\n{locked deps}\n\n=== Program.cs ===\n+var app = builder.Build();";

        _mockGitHub.Setup(x => x.GetPullRequestDiffAsync("owner", "repo", 1))
            .ReturnsAsync(diff);
        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("[]");

        await CreateOrchestrator().RunReviewAsync("owner", "repo", 1);

        _mockLlm.Verify(x => x.GetCompletionAsync(
            It.IsAny<string>(),
            It.Is<string>(s => !s.Contains("yarn.lock"))),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RunReviewAsync_WithMultipleAgentFindings_PostsOneSummaryComment()
    {
        var diff = "=== service.cs ===\n+public void Execute() { }";

        _mockGitHub.Setup(x => x.GetPullRequestDiffAsync("owner", "repo", 1))
            .ReturnsAsync(diff);

        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("""
                [
                    {"fileName":"service.cs","description":"Issue","severity":"Warning","category":"Quality","suggestion":"Fix","lineNumber":1}
                ]
                """);

        await CreateOrchestrator().RunReviewAsync("owner", "repo", 1);

        _mockGitHub.Verify(x => x.PostReviewCommentAsync(
            "owner", "repo", 1, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RunReviewAsync_WithNoFindings_PostsSummaryWithNoIssues()
    {
        var diff = "=== app.cs ===\n+// clean code";

        _mockGitHub.Setup(x => x.GetPullRequestDiffAsync("owner", "repo", 1))
            .ReturnsAsync(diff);
        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("[]");

        await CreateOrchestrator().RunReviewAsync("owner", "repo", 1);

        _mockGitHub.Verify(x => x.PostReviewCommentAsync(
            "owner", "repo", 1,
            It.Is<string>(s => s.Contains("No issues found"))),
            Times.Once);
    }

    [Theory]
    [InlineData("=== styles.min.css ===\n+body{}")]
    [InlineData("=== bundle.min.js ===\n+!function(){}()")]
    [InlineData("=== icon.svg ===\n+<svg/>")]
    [InlineData("=== PRReviewBot.Core.csproj ===\n+<Project/>")]
    public async Task RunReviewAsync_WithVaryingExcludedFileTypes_FiltersCorrectly(string excludedDiff)
    {
        var diff = excludedDiff + "\n\n=== logic.cs ===\n+public class Logic { }";

        _mockGitHub.Setup(x => x.GetPullRequestDiffAsync("owner", "repo", 1))
            .ReturnsAsync(diff);
        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("[]");

        await CreateOrchestrator().RunReviewAsync("owner", "repo", 1);

        _mockLlm.Verify(x => x.GetCompletionAsync(
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("logic.cs"))),
            Times.AtLeastOnce);
    }

    private ReviewOrchestrator CreateOrchestrator()
    {
        var agents = new List<IReviewAgent>
        {
            new SecurityAgent(_mockLlm.Object),
            new CodeQualityAgent(_mockLlm.Object),
            new PerformanceAgent(_mockLlm.Object),
            new ArchitectureAgent(_mockLlm.Object)
        };

        return new ReviewOrchestrator(_mockGitHub.Object, agents, new SummarizerAgent(_mockLlm.Object));
    }
}
