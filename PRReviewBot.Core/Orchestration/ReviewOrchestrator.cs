using PRReviewBot.Core.Agents;
using PRReviewBot.Core.Services;

namespace PRReviewBot.Core.Orchestration;

public class ReviewOrchestrator
{
    private readonly IGitHubService _gitHubService;
    private readonly List<IReviewAgent> _agents;
    private readonly SummarizerAgent _summarizer;
    
    public ReviewOrchestrator(
        IGitHubService gitHubService,
        IEnumerable<IReviewAgent> agents,
        SummarizerAgent summarizer)
    {
        _gitHubService = gitHubService;
        _agents = agents.ToList();
        _summarizer = summarizer;
    }
    
    public async Task RunReviewAsync(string owner, string repo, int pullRequestNumber)
    {
        var diff = await _gitHubService.GetPullRequestDiffAsync(owner, repo, pullRequestNumber);
        
        if (string.IsNullOrWhiteSpace(diff))
        {
            await _gitHubService.PostReviewCommentAsync(owner, repo, pullRequestNumber,
                "## AI Code Review\n\nNo code changes detected in this PR.");
            return;
        }

        var filteredDiff = FilterDiff(diff);
        
        var reviewTasks = _agents.Select(agent => agent.ReviewAsync(filteredDiff));
        var results = await Task.WhenAll(reviewTasks);
        
        var allFindings = results
            .SelectMany(findings => findings)
            .OrderByDescending(f => f.Severity)
            .ToList();

        var summary = await _summarizer.SummarizeAsync(allFindings);

        await _gitHubService.PostReviewCommentAsync(owner, repo, pullRequestNumber, summary);
    }
    
    private string FilterDiff(string diff)
    {
        var excludedExtensions = new HashSet<string>
        {
            ".png", ".jpg", ".jpeg", ".gif", ".svg", ".ico",
            ".lock", ".min.js", ".min.css",
            ".csproj", ".sln",
            ".md", ".txt"
        };

        var lines = diff.Split('\n');
        var filtered = new System.Text.StringBuilder();
        var includeCurrentFile = true;

        foreach (var line in lines)
        {
            if (line.StartsWith("=== "))
            {
                var fileName = line.Trim('=', ' ');
                includeCurrentFile = !excludedExtensions.Any(ext =>
                    fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
            }

            if (includeCurrentFile)
            {
                filtered.AppendLine(line);
            }
        }

        return filtered.ToString();
    }
}