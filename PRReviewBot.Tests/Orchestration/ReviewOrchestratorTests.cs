using Moq;
using PRReviewBot.Core.Agents;
using PRReviewBot.Core.Orchestration;
using PRReviewBot.Core.Services;

namespace PRReviewBot.Tests.Orchestration;

public class ReviewOrchestratorTests
{
    private readonly Mock<IGitHubService> _mockGitHub;
    private readonly Mock<ILlmService> _mockLlm;

    public ReviewOrchestratorTests()
    {
        _mockGitHub = new Mock<IGitHubService>();
        _mockLlm = new Mock<ILlmService>();
    }

    [Fact]
    public async Task RunReviewAsync_WithEmptyDiff_PostsNoChangesMessage()
    {
        _mockGitHub.Setup(x => x.GetPullRequestDiffAsync("owner", "repo", 1))
            .ReturnsAsync("");

        var orchestrator = CreateOrchestrator();

        await orchestrator.RunReviewAsync("owner", "repo", 1);

        _mockGitHub.Verify(x => x.PostReviewCommentAsync(
            "owner", "repo", 1,
            It.Is<string>(s => s.Contains("No code changes detected"))),
            Times.Once);
    }

    [Fact]
    public async Task RunReviewAsync_WithCodeDiff_RunsAllAgentsAndPostsReview()
    {
        var diff = "=== test.cs ===\n+public class Test { }";

        _mockGitHub.Setup(x => x.GetPullRequestDiffAsync("owner", "repo", 1))
            .ReturnsAsync(diff);

        _mockLlm.Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("[]");

        var orchestrator = CreateOrchestrator();

        await orchestrator.RunReviewAsync("owner", "repo", 1);

        _mockGitHub.Verify(x => x.PostReviewCommentAsync(
            "owner", "repo", 1, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RunReviewAsync_FiltersNonCodeFiles()
    {
        var diff = "=== readme.md ===\n+some text\n\n=== app.cs ===\n+real code";

        _mockGitHub.Setup(x => x.GetPullRequestDiffAsync("owner", "repo", 1))
            .ReturnsAsync(diff);

        _mockLlm.Setup(x => x.GetCompletionAsync(
            It.IsAny<string>(),
            It.Is<string>(s => !s.Contains("readme.md"))))
            .ReturnsAsync("[]");

        var orchestrator = CreateOrchestrator();

        await orchestrator.RunReviewAsync("owner", "repo", 1);

        _mockLlm.Verify(x => x.GetCompletionAsync(
            It.IsAny<string>(),
            It.Is<string>(s => !s.Contains("readme.md"))),
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

        var summarizer = new SummarizerAgent(_mockLlm.Object);

        return new ReviewOrchestrator(_mockGitHub.Object, agents, summarizer);
    }
}