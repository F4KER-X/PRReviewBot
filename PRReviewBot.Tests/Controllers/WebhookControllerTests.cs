using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PRReviewBot.Api.Controllers;
using PRReviewBot.Api.Models;
using PRReviewBot.Core.Agents;
using PRReviewBot.Core.Orchestration;
using PRReviewBot.Core.Services;

namespace PRReviewBot.Tests.Controllers;

public class WebhookControllerTests
{
    private readonly Mock<IReviewTracker> _trackerMock = new();
    private readonly Mock<IGitHubService> _githubMock = new();
    private readonly Mock<ILlmService> _llmMock = new();

    public WebhookControllerTests()
    {
        _githubMock
            .Setup(x => x.GetPullRequestDiffAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(string.Empty);
    }

    [Fact]
    public async Task HandleGitHubWebhook_WithNoWebhookSecret_ReturnsReviewStarted()
    {
        var controller = CreateController(secret: null);
        SetupRequest(controller, body: "{}");

        var result = await controller.HandleGitHubWebhook(CreatePayload("opened"));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Review started", ok.Value?.ToString());
    }

    [Fact]
    public async Task HandleGitHubWebhook_WithValidSignature_ReturnsReviewStarted()
    {
        const string secret = "my-secret";
        const string body = "{\"action\":\"opened\"}";

        var controller = CreateController(secret: secret);
        SetupRequest(controller, body, signature: ComputeSignature(secret, body));

        var result = await controller.HandleGitHubWebhook(CreatePayload("opened"));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Review started", ok.Value?.ToString());
    }

    [Fact]
    public async Task HandleGitHubWebhook_WithInvalidSignature_ReturnsUnauthorized()
    {
        var controller = CreateController(secret: "correct-secret");
        SetupRequest(controller, body: "{}", signature: "sha256=badhash");

        var result = await controller.HandleGitHubWebhook(CreatePayload("opened"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task HandleGitHubWebhook_WithMissingSignatureHeader_ReturnsUnauthorized()
    {
        var controller = CreateController(secret: "some-secret");
        SetupRequest(controller, body: "{}", signature: null);

        var result = await controller.HandleGitHubWebhook(CreatePayload("opened"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Theory]
    [InlineData("closed")]
    [InlineData("labeled")]
    [InlineData("reopened")]
    [InlineData("assigned")]
    public async Task HandleGitHubWebhook_WithIgnoredAction_ReturnsActionIgnored(string action)
    {
        var controller = CreateController(secret: null);
        SetupRequest(controller, body: "{}");

        var result = await controller.HandleGitHubWebhook(CreatePayload(action));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Action ignored", ok.Value?.ToString());
    }

    [Theory]
    [InlineData("opened")]
    [InlineData("synchronize")]
    public async Task HandleGitHubWebhook_WithSupportedActions_ReturnsReviewStarted(string action)
    {
        var controller = CreateController(secret: null);
        SetupRequest(controller, body: "{}");

        var result = await controller.HandleGitHubWebhook(CreatePayload(action));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Review started", ok.Value?.ToString());
    }

    [Fact]
    public async Task HandleGitHubWebhook_WithNullRepository_ReturnsBadRequest()
    {
        var controller = CreateController(secret: null);
        SetupRequest(controller, body: "{}");

        var payload = new GitHubWebhookPayload { Action = "opened", Number = 1, Repository = null };
        var result = await controller.HandleGitHubWebhook(payload);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task HandleGitHubWebhook_WithNullOwner_ReturnsBadRequest()
    {
        var controller = CreateController(secret: null);
        SetupRequest(controller, body: "{}");

        var payload = new GitHubWebhookPayload
        {
            Action = "opened",
            Number = 1,
            Repository = new RepositoryInfo { Name = "repo", Owner = null }
        };
        var result = await controller.HandleGitHubWebhook(payload);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task HandleGitHubWebhook_WithEmptyRepoName_ReturnsBadRequest()
    {
        var controller = CreateController(secret: null);
        SetupRequest(controller, body: "{}");

        var payload = new GitHubWebhookPayload
        {
            Action = "opened",
            Number = 1,
            Repository = new RepositoryInfo { Name = string.Empty, Owner = new OwnerInfo { Login = "owner" } }
        };
        var result = await controller.HandleGitHubWebhook(payload);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task HandleGitHubWebhook_WhenAlreadyReviewed_ReturnsAlreadyReviewed()
    {
        _trackerMock.Setup(x => x.HasBeenReviewed("owner", "repo", 1, "sha123"))
            .Returns(true);

        var controller = CreateController(secret: null);
        SetupRequest(controller, body: "{}");

        var result = await controller.HandleGitHubWebhook(CreatePayload("opened"));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Already reviewed", ok.Value?.ToString());
    }

    [Fact]
    public async Task HandleGitHubWebhook_WithNewPr_MarksAsReviewed()
    {
        var controller = CreateController(secret: null);
        SetupRequest(controller, body: "{}");

        await controller.HandleGitHubWebhook(CreatePayload("opened"));

        _trackerMock.Verify(x => x.MarkReviewed("owner", "repo", 1, "sha123"), Times.Once);
    }

    [Fact]
    public async Task HandleGitHubWebhook_WhenAlreadyReviewed_DoesNotMarkAgain()
    {
        _trackerMock.Setup(x => x.HasBeenReviewed("owner", "repo", 1, "sha123"))
            .Returns(true);

        var controller = CreateController(secret: null);
        SetupRequest(controller, body: "{}");

        await controller.HandleGitHubWebhook(CreatePayload("opened"));

        _trackerMock.Verify(x => x.MarkReviewed(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    private WebhookController CreateController(string? secret)
    {
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["GitHub:WebhookSecret"]).Returns(secret);

        var summarizer = new SummarizerAgent(_llmMock.Object);
        var orchestrator = new ReviewOrchestrator(_githubMock.Object, [], summarizer);

        var controller = new WebhookController(
            orchestrator,
            _trackerMock.Object,
            configMock.Object,
            NullLogger<WebhookController>.Instance);

        return controller;
    }

    private static void SetupRequest(WebhookController controller, string body, string? signature = null)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(bodyBytes);
        httpContext.Request.ContentLength = bodyBytes.Length;

        if (signature != null)
            httpContext.Request.Headers["X-Hub-Signature-256"] = signature;

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private static GitHubWebhookPayload CreatePayload(string action) => new()
    {
        Action = action,
        Number = 1,
        Repository = new RepositoryInfo { Name = "repo", Owner = new OwnerInfo { Login = "owner" } },
        PullRequest = new PullRequestInfo { Head = new HeadInfo { Sha = "sha123" } }
    };

    private static string ComputeSignature(string secret, string body)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(bodyBytes);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
