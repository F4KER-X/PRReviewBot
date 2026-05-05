using System.Collections.Concurrent;

namespace PRReviewBot.Core.Services;

public class ReviewTracker : IReviewTracker
{
    private readonly ConcurrentDictionary<string, bool> _reviewed = new();

    public bool HasBeenReviewed(string owner, string repo, int pullRequestNumber, string commitSha)
    {
        var key = $"{owner}/{repo}#{pullRequestNumber}@{commitSha}";
        return _reviewed.ContainsKey(key);
    }
    
    public void MarkReviewed(string owner, string repo, int pullRequestNumber, string commitSha)
    {
        var key = $"{owner}/{repo}#{pullRequestNumber}@{commitSha}";
        _reviewed.TryAdd(key, true);
    }
}