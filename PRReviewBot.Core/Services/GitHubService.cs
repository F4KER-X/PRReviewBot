using Octokit;
namespace PRReviewBot.Core.Services;

public class GitHubService : IGitHubService
{
    private readonly GitHubClient  _client;

    public GitHubService(string token)
    {
        _client = new GitHubClient(new ProductHeaderValue("PRReviewBot"))
        {
            Credentials = new Credentials(token)
        };
    }

    public async Task<string> GetPullRequestDiffAsync(string owner, string repo, int pullRequestNumber)
    {
        var files = await _client.PullRequest.Files(owner, repo, pullRequestNumber);
        
        var diffBuilder = new System.Text.StringBuilder();

        foreach (var file in files)
        {
            diffBuilder.AppendLine($"=== {file.FileName} ===");
            diffBuilder.AppendLine($"Status: {file.Status} | Changes: +{file.Additions} -{file.Deletions}");

            if (!string.IsNullOrEmpty(file.Patch))
            {
                diffBuilder.AppendLine(file.Patch);
            }
            
            diffBuilder.AppendLine();
        }
        return diffBuilder.ToString();
    }

    public async Task PostReviewCommentAsync(string owner, string repo, int pullRequestNumber, string body)
    {
        await _client.Issue.Comment.Create(owner, repo, pullRequestNumber, body);
    }
}