namespace PRReviewBot.Core.Services;

public interface IGitHubService
{
    Task<string> GetPullRequestDiffAsync(string owner, string repo, int pullRequestNumber);
    Task PostReviewCommentAsync(string owner, string repo, int pullRequestNumber, string body);
}