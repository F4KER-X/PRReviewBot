using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using PRReviewBot.Api.Models;
using PRReviewBot.Core.Orchestration;
using PRReviewBot.Core.Services;

namespace PRReviewBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly ReviewOrchestrator _orchestrator;
    private readonly IReviewTracker _reviewTracker;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        ReviewOrchestrator orchestrator,
        IReviewTracker reviewTracker,
        IConfiguration configuration,
        ILogger<WebhookController> logger)
    {
        _orchestrator = orchestrator;
        _reviewTracker = reviewTracker;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("github")]
    public async Task<IActionResult> HandleGitHubWebhook([FromBody] GitHubWebhookPayload payload)
    {
        if (!await ValidateWebhookSignature())
        {
            _logger.LogWarning("Invalid webhook signature received");
            return Unauthorized("Invalid signature");
        }

        if (payload.Action != "opened" && payload.Action != "synchronize")
        {
            _logger.LogInformation("Ignoring action: {Action}", payload.Action);
            return Ok(new { message = "Action ignored" });
        }

        var owner = payload.Repository?.Owner?.Login;
        var repo = payload.Repository?.Name;
        var prNumber = payload.Number;
        var commitSha = payload.PullRequest?.Head?.Sha ?? "";

        if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
        {
            _logger.LogWarning("Missing repository information in webhook payload");
            return BadRequest("Missing repository information");
        }
        
        if (_reviewTracker.HasBeenReviewed(owner, repo, prNumber, commitSha))
        {
            _logger.LogInformation("Already reviewed {Owner}/{Repo} PR #{PrNumber} at {Sha}",
                owner, repo, prNumber, commitSha);
            return Ok(new { message = "Already reviewed" });
        }
        
        _reviewTracker.MarkReviewed(owner, repo, prNumber, commitSha);

        _logger.LogInformation("Starting review for {Owner}/{Repo} PR #{PrNumber}",
            owner, repo, prNumber);

        _ = Task.Run(async () =>
        {
            try
            {
                await _orchestrator.RunReviewAsync(owner, repo, prNumber);
                _logger.LogInformation("Review completed for {Owner}/{Repo} PR #{PrNumber}",
                    owner, repo, prNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Review failed for {Owner}/{Repo} PR #{PrNumber}",
                    owner, repo, prNumber);
            }
        });

        return Ok(new { message = "Review started" });
    }

    private async Task<bool> ValidateWebhookSignature()
    {
        var secret = _configuration["GitHub:WebhookSecret"];

        if (string.IsNullOrEmpty(secret))
            return true;

        if (!Request.Headers.TryGetValue("X-Hub-Signature-256", out var signatureHeader))
            return false;

        Request.Body.Position = 0;
        var body = await new StreamReader(Request.Body).ReadToEndAsync();
        Request.Body.Position = 0;

        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(bodyBytes);
        var expectedSignature = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();

        return signatureHeader.ToString() == expectedSignature;
    }
}