namespace PRReviewBot.Core.Services;

public interface IReviewTracker
{
    bool HasBeenReviewed(string owner, string repo, int pullRequestNumber, string commitSha);
    void MarkReviewed(string owner, string repo, int pullRequestNumber, string commitSha);
}